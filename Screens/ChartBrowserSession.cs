using ProjectOdyssey.Audio;
using ProjectOdyssey.IO;
using System.Runtime.CompilerServices;

namespace ProjectOdyssey.Screens
{
    public class ChartBrowserSession
    {
        private List<List<ChartBrowserRow>> chartSets = new(); // All chart sets, each set is a list of charts
        private AudioManager audioManager;
        public event Action<string>? SelectionChanged;

        public int ChartSetCursor { get; private set; } // Currently selected chart set index
        public int ChartCursor { get; private set; } // Currently selected chart within the current set as an index
        public int HoveredSet { get; private set; } = -1; // The set card currently hovered by the mouse, or -1 if none
        public int HoveredChart { get; private set; } = -1; // The chart card currently hovered by the mouse, or -1 if none

        public List<ChartBrowserRow> CurrentSet => chartSets[ChartSetCursor]; // The currently selected chart set
        public List<List<ChartBrowserRow>> FilteredChartSets => chartSets; // The chart sets after filtering (currently no filtering applied)
        public List<(int Slot, CardRect Rect)> SetCardRects { get; } = new(); // Rectangle positions and dimensions of the set cards on screen
        public List<(int ChartIndex, CardRect Rect)> ChartCardRects { get; } = new(); // Rectangle positions and dimensions of the chart cards on screen
       
        public ChartBrowserSession(List<ChartBrowserRow> charts, AudioManager audioManager)
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

        // Acts on whatever UpdateHover last determined is hovered — a chart card
        // takes priority over a set card underneath it.
        public void SelectHovered()
        {
            if (HoveredChart != -1) 
            { 
                SelectChart(ChartCardRects[HoveredChart].ChartIndex); 
                return;
            }

            if (HoveredSet != -1) 
            { 
                MoveSet(SetCardRects[HoveredSet].Slot); 
                return; 
            }
        }

        // LogicalPos must already be in the fixed 1920x1080 space.
        public void UpdateHover(OpenTK.Mathematics.Vector2 logicalPos)
        {
            HoveredChart = -1;
            for (int i = 0; i < ChartCardRects.Count; i++)
            {
                if (ChartCardRects[i].Rect.Contains(logicalPos.X, logicalPos.Y)) { HoveredChart = i; break; }
            }

            HoveredSet = -1;
            if (HoveredChart == -1)
            {
                for (int i = 0; i < SetCardRects.Count; i++)
                {
                    if (SetCardRects[i].Rect.Contains(logicalPos.X, logicalPos.Y)) { HoveredSet = i; break; }
                }
            }
        }

        // Returns the vertical spacing before a row, which is different for set cards and chart cards.
        private static float SpacingBefore(Row row)
        {
            if (row.IsChart)
            {
                return ChartBrowserLayout.ChartCardSpacing;
            }
            else 
            {
                return ChartBrowserLayout.SetCardSpacing;
            }
        }

        // Builds the layout of set and chart cards, calculating their positions and sizes, and storing them in SetCardRects and ChartCardRects.
        private void RebuildLayout()
        {
            SetCardRects.Clear();
            ChartCardRects.Clear();

            if (chartSets.Count == 0) return;

            var rows = new List<Row>();

            // Add rows for the selected set and its charts, plus a few sets above and below it.
            for (int slot = -5; slot <= 5; slot++)
            {
                int index = ChartSetCursor + slot;
                if (index < 0 || index >= chartSets.Count) continue;

                // Adds a row for the set card
                rows.Add(new Row { IsChart = false, SetSlot = slot, Height = ChartBrowserLayout.SetCardHeight });

                // For identified set, add rows for each of its charts
                if (index == ChartSetCursor)
                {
                    var set = chartSets[index];
                    for (int c = 0; c < set.Count; c++)
                    {
                        rows.Add(new Row { IsChart = true, ChartIndex = c, Height = ChartBrowserLayout.ChartCardHeight });
                    }
                }
            }

            // Finds the index of the row corresponding to the currently selected chart, which will be vertically centred.
            int anchorIndex = rows.FindIndex(r => r.IsChart && r.ChartIndex == ChartCursor);

            // Calculates the Y positions of all rows, starting from the centered anchor row and moving outward.
            float[] tops = new float[rows.Count];
            tops[anchorIndex] = 1080 / 2f - rows[anchorIndex].Height / 2f;

            for (int i = anchorIndex + 1; i < rows.Count; i++)
            {
                tops[i] = tops[i - 1] + rows[i - 1].Height + SpacingBefore(rows[i]);
            }

            for (int i = anchorIndex - 1; i >= 0; i--)
            {
                tops[i] = tops[i + 1] - rows[i].Height - SpacingBefore(rows[i + 1]);
            }

            float setX = ChartBrowserLayout.SetCardX(1920);
            float chartX = ChartBrowserLayout.ChartCardX(1920);

            // Creates the rectangles for each row, storing them in SetCardRects or ChartCardRects as appropriate.
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (!r.IsChart)
                {
                    SetCardRects.Add((r.SetSlot, new CardRect
                    {
                        X = setX,
                        Y = tops[i],
                        Width = ChartBrowserLayout.SetCardWidth,
                        Height = ChartBrowserLayout.SetCardHeight
                    }));
                }
                else
                {
                    ChartCardRects.Add((r.ChartIndex, new CardRect
                    {
                        X = chartX,
                        Y = tops[i],
                        Width = ChartBrowserLayout.ChartCardWidth,
                        Height = ChartBrowserLayout.ChartCardHeight
                    }));
                }
            }

            SelectionChanged?.Invoke(CurrentSet[ChartCursor].BackgroundPath);
            PlaySelectedChartMusic();
        }

        public void PlaySelectedChartMusic()
        {
            if (chartSets.Count == 0) return;

            string audioPath = CurrentSet[ChartCursor].AudioPath;
            audioManager.ReadAudioFile(audioPath);
            audioManager.PlayAudio(audioPath);
        }

        private struct Row
        {
            public bool IsChart;
            public int SetSlot;     // Set rows only: slot relative to ChartSetCursor
            public int ChartIndex;  // Chart rows only: absolute index into CurrentSet
            public float Height;
        }
    }
}
