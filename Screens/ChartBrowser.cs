using ProjectOdyssey.IO;
using ProjectOdyssey.Render;

namespace ProjectOdyssey.Screens
{
    class ChartBrowser : IGameScreen
    {
        private ChartBrowserRenderer browserRenderer = null!;
        private List<ChartBrowserRow> charts = new List<ChartBrowserRow>();
        private int chartCursor;

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

            Random rand = new Random(DateTime.Now.ToString().GetHashCode());
            chartCursor = rand.Next(0, charts.Count);

            Console.WriteLine($"[INFO] Loaded {charts.Count} charts for browsing.");
            Console.WriteLine($"[INFO] Starting chart cursor at index {chartCursor}.");
            Console.WriteLine($"[INFO] Selected Chart: ID: {charts[chartCursor].ChartId}, Title: {charts[chartCursor].Title}, Diff: {charts[chartCursor].DiffName}.");
        }

        public void Update(float deltaMs)
        {
        }

        public void Render()
        {
            browserRenderer.Draw(charts);
        }

        public void Resize(int width, int height)
        {
            browserRenderer.Resize(width, height);
        }

        public void Unload()
        {
            browserRenderer.Dispose();
        }
    }
}