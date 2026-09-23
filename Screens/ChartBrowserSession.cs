using ProjectOdyssey.IO;

namespace ProjectOdyssey.Screens
{
    public class ChartBrowserSession
    {
        // One row in the flattened, continuous vertical list this screen renders:
        // ..., Set(k-1), Set(k), Chart(k,0), ..., Chart(k,N-1), Set(k+1), ...
        // where k is the currently selected set. Only the selected set expands
        // into its charts inline — every other set is a single row.
        private struct Row
        {
            public bool IsChart;
            public int SetSlot;     // Set rows only: slot relative to ChartSetCursor
            public int ChartIndex;  // Chart rows only: absolute index into CurrentSet
            public float Height;
        }

        private List<List<ChartBrowserRow>> chartSets = new();

        public int ChartSetCursor { get; private set; }
        public int ChartCursor { get; private set; }

        public List<ChartBrowserRow> CurrentSet => chartSets[ChartSetCursor];
        public List<List<ChartBrowserRow>> ChartSets => chartSets;

        public List<(int Slot, CardRect Rect)> SetCardRects { get; } = new();
        public List<(int ChartIndex, CardRect Rect)> ChartCardRects { get; } = new();

        public int HoveredSet { get; private set; } = -1;
        public int HoveredChart { get; private set; } = -1;

        public ChartBrowserSession(List<ChartBrowserRow> charts)
        {
            if (charts.Count == 0) return;

            chartSets = charts
                .GroupBy(c => c.SetId)
                .Select(g => g.ToList())
                .ToList();

            ChartSetCursor = Random.Shared.Next(chartSets.Count);
            RebuildLayout();
        }

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
            if (HoveredChart != -1) { SelectChart(ChartCardRects[HoveredChart].ChartIndex); return; }
            if (HoveredSet != -1) { MoveSet(SetCardRects[HoveredSet].Slot); return; }
        }

        // logicalPos must already be in the fixed 1920x1080 space.
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

        // Spacing between two adjacent rows depends only on what the LATER row
        // is: a chart row leaves a small gap, a set row leaves a bigger one.
        // This one rule correctly covers every transition (set->set, set->first
        // chart, chart->chart, last chart->next set).
        private static float SpacingBefore(Row row) =>
            row.IsChart ? ChartBrowserLayout.ChartCardSpacing : ChartBrowserLayout.SetCardSpacing;

        // The whole set/chart list is flattened into one ordered sequence of
        // rows, then positioned by walking outward from the selected chart —
        // which is always pinned to the vertical centre of the screen. Rows
        // above/below simply overflow past the top/bottom of the screen; that's
        // fine, nothing needs to fit.
        private void RebuildLayout()
        {
            SetCardRects.Clear();
            ChartCardRects.Clear();

            if (chartSets.Count == 0) return;

            var rows = new List<Row>();
            for (int slot = -5; slot <= 5; slot++)
            {
                int index = ChartSetCursor + slot;
                if (index < 0 || index >= chartSets.Count) continue;

                rows.Add(new Row { IsChart = false, SetSlot = slot, Height = ChartBrowserLayout.SetCardHeight });

                if (index == ChartSetCursor)
                {
                    var set = chartSets[index];
                    for (int c = 0; c < set.Count; c++)
                    {
                        rows.Add(new Row { IsChart = true, ChartIndex = c, Height = ChartBrowserLayout.ChartCardHeight });
                    }
                }
            }

            int anchorIndex = rows.FindIndex(r => r.IsChart && r.ChartIndex == ChartCursor);
            if (anchorIndex == -1) return; // shouldn't happen — selected set is always included as slot 0

            float[] tops = new float[rows.Count];
            tops[anchorIndex] = 1080 / 2f - rows[anchorIndex].Height / 2f;

            for (int i = anchorIndex + 1; i < rows.Count; i++)
                tops[i] = tops[i - 1] + rows[i - 1].Height + SpacingBefore(rows[i]);

            for (int i = anchorIndex - 1; i >= 0; i--)
                tops[i] = tops[i + 1] - rows[i].Height - SpacingBefore(rows[i + 1]);

            float setX = ChartBrowserLayout.SetCardX(1920);
            float chartX = ChartBrowserLayout.ChartCardX(1920);

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
        }
    }
}
