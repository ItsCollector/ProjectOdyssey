using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Input;
using ProjectOdyssey.Input.Native;
using ProjectOdyssey.IO;
using ProjectOdyssey.Render;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens
{
    class ChartBrowserScreen : IGameScreen
    {
        private ChartBrowserRenderer browserRenderer = null!;
        private ChartBrowserSession session = null!;

        private int windowWidth = 1920, windowHeight = 1080;
        private Vector2 lastMousePos;
        private InputHistory inputHistory;

        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }

        public ChartBrowserScreen(InputHistory inputHistory)
        {
            this.inputHistory = inputHistory;
        }

        public void Load()
        {
            browserRenderer = new ChartBrowserRenderer(SkinManager.MenuSkin, Renderer);

            var charts = ChartDatabase.GetChartsForBrowsing();

            if (charts.Count == 0)
            {
                Console.WriteLine("[WARN] No charts found to browse.");
            }

            session = new ChartBrowserSession(charts, AudioManager);
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
        }

        public void Unload()
        {
            // Unsubscribe first so nothing can call into a disposed renderer
            session.SelectionChanged -= browserRenderer.LoadBackgroundTexture;
            browserRenderer.Dispose();   // frees the chart background texture it loaded
        }

        private void CommitSelection()
        {
            session.StopSelectedChartMusic();

            var (chartData, songPath) = session.FinaliseChartSelection();
            if (chartData != null)
            {
                ScreenManager.Replace(new GameplayScreen(chartData, songPath, inputHistory));
            }
        }

        public void OnKeyDown(Keys key)
        {
            switch (key)
            {
                case Keys.Up:
                    session.MoveSet(-1); break;
                case Keys.Down:
                    session.MoveSet(1); break;
                case Keys.Left:
                    session.MoveChart(-1); break;
                case Keys.Right:
                    session.MoveChart(1); break;
                case Keys.Enter:
                    CommitSelection();
                    break;
            }
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button != MouseButton.Left) return;

            if (session.IsHoveredChartAlreadySelected)
            {
                CommitSelection();
                return;
            }

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
