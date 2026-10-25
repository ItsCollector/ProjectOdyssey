using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.App;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Common;
using ProjectOdyssey.Data.Charts;
using ProjectOdyssey.Data.Replays;
using ProjectOdyssey.Data.Scores;
using ProjectOdyssey.Input;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.Screens.Gameplay;
using ProjectOdyssey.Screens.Results;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens.ChartBrowser
{
    class ChartBrowserScreen : IGameScreen
    {
        private ChartBrowserView browserRenderer = null!;
        private ChartBrowserState session = null!;

        private int windowWidth = 1920, windowHeight = 1080;
        private Vector2 lastMousePos;
        private InputHistory inputHistory;

        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }

        public ChartBrowserScreen(InputHistory inputHistory, Renderer renderer, ScreenManager screenManager, AudioManager audioManager, SkinManager skinManager)
        {
            this.inputHistory = inputHistory;
            Renderer = renderer;
            ScreenManager = screenManager;
            AudioManager = audioManager;
            SkinManager = skinManager;
        }

        public void Load()
        {
            browserRenderer = new ChartBrowserView(SkinManager.MenuSkin, Renderer);

            var charts = ChartDatabase.GetChartsForBrowsing();

            if (charts.Count == 0)
            {
                Console.WriteLine("[WARN] No charts found to browse.");
            }

            session = new ChartBrowserState(charts, AudioManager);
            session.SelectionChanged += browserRenderer.RequestBackgroundTexture;

            if (charts.Count > 0)
            {
                browserRenderer.RequestBackgroundTexture(session.CurrentSet[session.ChartCursor].BackgroundPath);
            }
        }

        public void Update(float deltaMs) 
        {
            session.Update(deltaMs);

            if (AudioManager.PlayingPath == null)
            {
                session.RestartSelectedChartMusic();
            }
        }

        public void Render()
        {
            browserRenderer.DrawChartBrowser(
                session.Items, 
                session.FilteredChartSets,             
                session.ChartSetCursor, 
                session.ChartCursor, 
                session.HoveredItem);
        }

        public void Resize(int width, int height)
        {
            windowWidth = width;
            windowHeight = height;
        }

        public void Unload()
        {
            // Unsubscribe first so nothing can call into a disposed renderer
            session.SelectionChanged -= browserRenderer.RequestBackgroundTexture;
            browserRenderer.Dispose();   // frees the chart background texture it loaded
        }

        private void CommitSelection()
        {
            session.StopSelectedChartMusic();

            var (chartData, songPath) = session.FinaliseChartSelection();
            if (chartData != null)
            {
                ScreenManager.Push(new GameplayScreen(chartData, songPath, inputHistory, Renderer, ScreenManager, AudioManager, SkinManager));
            }
        }

        private void ViewScore()
        {
            int chartId = session.CurrentSet[session.ChartCursor].ChartId;
            Result<string> bestReplayPath = ScoreDatabase.GetBestReplayByChartId(chartId);

            if (bestReplayPath.IsSuccess == false)
            {
                Console.WriteLine($"[ERROR] {bestReplayPath.Error}");
                return;
            }

            ReplayData replayData;
            try 
            { 
                replayData = ReplayService.ReadReplayBinary(bestReplayPath.Value); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Could not read replay: {ex.Message}");
                return;
            }

            var (chartData, songPath) = session.FinaliseChartSelection();

            AudioManager.StopAudio();
            ScreenManager.Push(new ResultsScreen(replayData, chartData, Renderer, ScreenManager, AudioManager, SkinManager));
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

            if (session.ViewScoreHovered)
            {
                ViewScore();
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
