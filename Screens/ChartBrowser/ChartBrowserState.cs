using OpenTK.Mathematics;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Engine.Scoring;
using ProjectOdyssey.Data.Charts;
using ProjectOdyssey.Data.Scores;
using ProjectOdyssey.Engine.Chart;
using ProjectOdyssey.Screens.Components;

namespace ProjectOdyssey.Screens.ChartBrowser
{
    public class ChartBrowserState
    {
        public const int SetKind = 0;
        public const int ChartKind = 1;   // what a StackItem represents (stored in StackItem.Kind)

        private const float SettleDelayMs = 500; // how long the cursor must be still before the background and preview music load

        private List<List<ChartBrowserRow>> chartSets = new();  // all chart sets, each set is a list of charts
        private readonly AudioManager audioManager;

        private float msSinceSelectionChanged;
        private bool selectionPending;
        private Vector2 lastLogicalPos = new(-1, -1); // last mouse position, off-screen until the mouse first moves

        public StackView chartsStackView = new StackView(1080f);

        // TESTING - SCORES VIA STACK VIEW
        public StackView scoreStackView = new StackView(600f);
        public ScoreData scoreData;

        public event Action<string>? SelectionChanged; // raised with the background path once the selection has settled

        // Selection
        public int ChartSetCursor { get; private set; } // currently selected chart set index
        public int ChartCursor { get; private set; } // currently selected chart within the current set, as an index
        public List<ChartBrowserRow> CurrentSet => chartSets[ChartSetCursor]; // the currently selected chart set
        public List<List<ChartBrowserRow>> FilteredChartSets => chartSets; // the chart sets after filtering (currently no filtering applied)
        public List<ScoreRecord> CurrentScores = new(100);
        public ScoreRecord HoveredScoreRecord;

        // Layout
        public IReadOnlyList<StackItem> chartStackItems => chartsStackView.Items; // the cards to draw, with their animated positions
        public IReadOnlyList<StackItem> scoreStackItems => scoreStackView.Items;

        // Hover
        public StackItem? HoveredItem { get; private set; } // the card under the mouse, or null
        public bool ViewScoreHovered { get; private set; } = false;
        public CardRect ViewScoreRect { get; } = new CardRect { X = 0, Y = 450, Width = 650, Height = 100 };

        // True when the mouse is hovering the chart that's already the active selection, as opposed to
        // hovering a different chart, which should just move the cursor rather than commit anything.
        public bool IsHoveredChartAlreadySelected => HoveredItem is { Kind: ChartKind } hovered && hovered.Tag == ChartCursor;

        public ChartBrowserState(List<ChartBrowserRow> charts, AudioManager audioManager)
        {
            this.audioManager = audioManager;

            if (charts.Count == 0) return;

            // Loads all charts into chartSets, grouped by SetId. Each set is a list of charts.
            chartSets = charts
                .GroupBy(c => c.SetId)
                .Select(g => g.ToList())
                .ToList();

            ChartSetCursor = Random.Shared.Next(chartSets.Count);
            RebuildLayout();
        }

        // Call every frame. Animates the cards, then runs the heavy selection work once the cursor has been still for SettleDelayMs.
        public void Update(float deltaMs)
        {
            chartsStackView.Update(deltaMs);
            scoreStackView.Update(deltaMs);

            UpdateHover(lastLogicalPos);   // cards move under a still mouse, so re-test every frame

            if (!selectionPending) return;

            msSinceSelectionChanged += deltaMs;
            if (msSinceSelectionChanged >= SettleDelayMs)
            {
                SettleNow();
            }
        }

        // Applies the current selection immediately (background + preview music)
        public void SettleNow()
        {
            if (chartSets.Count == 0) return;

            selectionPending = false;
            SelectionChanged?.Invoke(CurrentSet[ChartCursor].BackgroundPath);
            PlaySelectedChartMusic();
            RefreshScores();
        }

        // Moves the set cursor by delta (clamped to valid range), resets the chart cursor to 0, and rebuilds the layout.
        public void MoveSet(int delta)
        {
            if (chartSets.Count == 0) return;

            int newCursor = Math.Clamp(ChartSetCursor + delta, 0, chartSets.Count - 1);
            if (newCursor == ChartSetCursor) return;

            ChartSetCursor = newCursor;
            ChartCursor = 0;
            RebuildLayout();
        }

        // Selects a chart by its real index within the current set (clamped),
        // then rebuilds the layout so that chart re-centres on screen.
        public void SelectChart(int index)
        {
            if (chartSets.Count == 0) return;

            ChartCursor = Math.Clamp(index, 0, CurrentSet.Count - 1);
            RebuildLayout();
        }

        public void MoveChart(int delta) => SelectChart(ChartCursor + delta);

        // LogicalPos must already be in the fixed 1920x1080 space.
        public void UpdateHover(Vector2 logicalPos)
        {
            lastLogicalPos = logicalPos;
            HoveredItem = null;

            foreach (var item in chartsStackView.Items)
            {
                if (!item.Contains(logicalPos.X, logicalPos.Y)) continue;

                HoveredItem = item;
                if (item.Kind == ChartKind) break;   // a chart card wins over a set card it overlaps
            }

            foreach (var item in scoreStackView.Items)
            {
                if (!item.Contains(logicalPos.X, logicalPos.Y)) continue;

                HoveredItem = item;
                ViewScoreHovered = true;
                HoveredScoreRecord = CurrentScores[item.Tag];
            }

            if (HoveredItem == null)
            {
                ViewScoreHovered = false;
            }
        }

        // Acts on whatever UpdateHover last determined is hovered. A chart card takes priority over a set card underneath it.
        public void SelectHovered()
        {
            if (HoveredItem == null) return;

            if (HoveredItem.Kind == ChartKind) SelectChart(HoveredItem.Tag);
            else MoveSet(HoveredItem.Tag);       // a set row's tag is its slot, i.e. the delta from the cursor
        }

        public void PlaySelectedChartMusic()
        {
            if (chartSets.Count == 0) return;

            string audioPath = CurrentSet[ChartCursor].AudioPath;
            audioManager.ReadAudioFile(audioPath);
            audioManager.PlayAudio(audioPath);
        }

        public void RestartSelectedChartMusic() => PlaySelectedChartMusic();

        public void StopSelectedChartMusic()
        {
            if (chartSets.Count == 0) return;

            audioManager.StopAudio();
        }

        public (ChartData, string) FinaliseChartSelection()
        {
            // get chart data
            string selectedChartPath = CurrentSet[ChartCursor].BinaryFilePath;
            ChartData chartData = ChartBinaryReader.ReadChartBinary(selectedChartPath, CurrentSet[ChartCursor].SetId, CurrentSet[ChartCursor].ChartId);

            // get song
            string selectedSongPath = CurrentSet[ChartCursor].AudioPath;
            chartData.SongFilePath = selectedSongPath;

            return (chartData, selectedSongPath);
        }

        // Stack item keys must be unique within one stack: the high bit keeps a chart ID from colliding with a set ID.
        private static long SetKey(int setId) => setId;
        private static long ChartKey(int chartId) => (1L << 32) | (uint)chartId;
        // Describes the visible set and chart cards to the stack view, which works out their positions
        // (chart under the cursor centred) and animates them there.
        private void RebuildLayout()
        {
            chartsStackView.Begin();
            int anchorIndex = -1;

            float setX = ChartBrowserLayout.SetCardX(1920);
            float chartX = ChartBrowserLayout.ChartCardX(1920);

            for (int slot = -5; slot <= 5; slot++)
            {
                int index = ChartSetCursor + slot;
                if (index < 0 || index >= chartSets.Count) continue;

                var set = chartSets[index];
                
                StackItem setItem = chartsStackView.Add(
                    SetKey(set[0].SetId), 
                    setX, 
                    ChartBrowserLayout.SetCardWidth,
                    ChartBrowserLayout.SetCardHeight, 
                    ChartBrowserLayout.SetCardSpacing,
                    tag: slot, 
                    kind: SetKind);

                if (index != ChartSetCursor) continue;

                for (int c = 0; c < set.Count; c++)
                {
                    if (c == ChartCursor)
                    {
                        anchorIndex = chartsStackView.Count;   // the index this chart is about to get
                    }

                    chartsStackView.Add(
                        ChartKey(set[c].ChartId), 
                        chartX, 
                        ChartBrowserLayout.ChartCardWidth,
                        ChartBrowserLayout.ChartCardHeight, 
                        ChartBrowserLayout.ChartCardSpacing,
                        tag: c, 
                        kind: ChartKind,
                        spawnFrom: setItem);
                }
            }

            chartsStackView.End(anchorIndex);
            UpdateHover(lastLogicalPos);

            selectionPending = true;
            msSinceSelectionChanged = 0f;
        }

        private void RefreshScores()
        {
            if (chartSets.Count == 0) return;

            try
            {
                CurrentScores = ScoreDatabase.GetScoresByChartId(CurrentSet[ChartCursor].ChartId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Could not load scores: {ex.Message}");
                CurrentScores = new List<ScoreRecord>();
            }

            scoreStackView.Begin();
            for (int i = 0; i < CurrentScores.Count; i++)
            {
                scoreStackView.Add(
                    CurrentScores[i].ScoreId, 
                    0,
                    ChartBrowserLayout.ScoreCardWidth,
                    ChartBrowserLayout.ScoreCardHeight,
                    ChartBrowserLayout.ScoreCardSpacing,
                    tag: i); // index into CurrentScores
            }

            scoreStackView.End(0);
        }
    }
}