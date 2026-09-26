using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.IO;
using ProjectOdyssey.Render;
using ProjectOdyssey.Audio;

namespace ProjectOdyssey.Screens
{
    class ChartBrowser : IGameScreen
    {
        private ChartBrowserRenderer browserRenderer = null!;
        private ChartBrowserSession session = null!;

        private int windowWidth = 1920, windowHeight = 1080;
        private Vector2 lastMousePos;
        private AudioManager audioManager;

        public ChartBrowser(AudioManager audioManager)
        {
            this.audioManager = audioManager;
        }

        public void Load()
        {
            browserRenderer = new ChartBrowserRenderer();
            browserRenderer.Initialise();

            var charts = ChartDatabase.GetChartsForBrowsing();

            if (charts.Count == 0)
            {
                Console.WriteLine("[WARN] No charts found to browse.");
            }

            session = new ChartBrowserSession(charts, audioManager);
            session.SelectionChanged += browserRenderer.LoadBackgroundTexture;

            if (charts.Count > 0)
            {
                browserRenderer.LoadBackgroundTexture(session.CurrentSet[session.ChartCursor].BackgroundPath);
            }
        }

        public void Update(float deltaMs) { }

        public void Render()
        {
            browserRenderer.DrawChartBrowser(
                session.SetCardRects,
                session.ChartCardRects,
                session.FilteredChartSets,
                session.ChartSetCursor,
                session.ChartCursor,
                session.HoveredSet,
                session.HoveredChart);
        }

        public void Resize(int width, int height)
        {
            windowWidth = width;
            windowHeight = height;
            browserRenderer.Resize(width, height);
        }

        public void Unload()
        {
            browserRenderer.Dispose();
            session.SelectionChanged -= browserRenderer.LoadBackgroundTexture;
        }

        public void OnKeyDown(Keys key)
        {
            switch (key)
            {
                case Keys.Up: session.MoveSet(-1); break;
                case Keys.Down: session.MoveSet(1); break;
                case Keys.Left: session.MoveChart(-1); break;
                case Keys.Right: session.MoveChart(1); break;
            }
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button != MouseButton.Left) return;
            session.SelectHovered();
        }

        public void OnMouseMove(MouseMoveEventArgs e)
        {
            lastMousePos = e.Position;
            session.UpdateHover(ToLogical(lastMousePos));
        }

        public void OnMouseWheel(MouseWheelEventArgs e)
        {
            session.MoveSet(-(int)e.OffsetY);
        }

        private Vector2 ToLogical(Vector2 windowPos) =>
            new Vector2(windowPos.X * 1920f / windowWidth, windowPos.Y * 1080f / windowHeight);
    }
}
