using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.IO;
using ProjectOdyssey.Render;

namespace ProjectOdyssey.Screens
{
    class ChartBrowser : IGameScreen
    {
        private ChartBrowserRenderer browserRenderer = null!;
        private List<ChartBrowserRow> charts = new List<ChartBrowserRow>();
        private List<List<ChartBrowserRow>> chartSets = new List<List<ChartBrowserRow>>();
        private List<List<ChartBrowserRow>> visibleSets = new List<List<ChartBrowserRow>>();

        private int chartSetCursor;
        private int chartCursor;

        private List<ChartBrowserRow> CurrentSet => chartSets[chartSetCursor];
        private ChartBrowserRow CurrentChart => CurrentSet[chartCursor];

        public void Load()
        {
            browserRenderer = new ChartBrowserRenderer();
            browserRenderer.Initialise();

            charts = ChartDatabase.GetChartsForBrowsing();

            if (charts.Count == 0)
            {
                Console.WriteLine("[WARN] No charts found to browse.");
                return;
            }

            // One entry per set; each entry holds that set's charts
            chartSets = charts
                .GroupBy(c => c.SetId)
                .Select(g => g.ToList())
                .ToList();

            chartSetCursor = Random.Shared.Next(chartSets.Count);
            RefreshVisibleSets();
        }

        // Refresh the visible sets based on the current set cursor, showing 5 sets before and after the current set
        private void RefreshVisibleSets()
        {
            visibleSets.Clear();

            for (int i = -5; i <= 5; i++)
            {
                int index = chartSetCursor + i;
                if (index < 0 || index >= chartSets.Count) continue;
                visibleSets.Add(chartSets[index]);
            }

            Console.WriteLine($"[INFO] Current set cursor: {chartSetCursor}, chart cursor: {chartCursor}, chart count: {CurrentSet.Count}");
        }

        // Navigate between sets based on direction
        private void MoveSet(int delta)
        {
            if (chartSets.Count == 0) return;

            int newCursor = Math.Clamp(chartSetCursor + delta, 0, chartSets.Count - 1);
            if (newCursor == chartSetCursor) return;

            chartSetCursor = newCursor;
            chartCursor = 0; // first chart in the newly selected set
            RefreshVisibleSets();
        }

        // Navigate between charts within the current set based on direction
        private void MoveChart(int delta)
        {
            if (chartSets.Count == 0) return;

            chartCursor = Math.Clamp(chartCursor + delta, 0, CurrentSet.Count - 1);
            Console.WriteLine($"[DEBUG] chartCursor = {chartCursor} / {CurrentSet.Count - 1}: {CurrentChart.DiffName}");
        }

        public void Update(float deltaMs)
        { 

        }

        public void Render()
        {
            browserRenderer.DrawChartBrowser(charts);
        }

        public void Resize(int width, int height)
        {
            browserRenderer.Resize(width, height);
        }

        public void Unload()
        {
            browserRenderer.Dispose();
        }

        // keyboard: up/down = sets, left/right = charts within the set
        public void OnKeyDown(Keys key)
        {
            switch (key)
            {
                case Keys.Up: MoveSet(-1); break;
                case Keys.Down: MoveSet(1); break;
                case Keys.Left: MoveChart(-1); break;
                case Keys.Right: MoveChart(1); break;
            }
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            Console.WriteLine("[INFO] " + e.Button + " mouse button pressed");
        }

        public void OnMouseMove(MouseMoveEventArgs e)
        {
            //Console.WriteLine("[INFO] Mouse moved to position: " + e.Position);
        }

        public void OnMouseWheel(MouseWheelEventArgs e)
        {
            MoveSet(-(int)e.OffsetY);
        }

    }
}