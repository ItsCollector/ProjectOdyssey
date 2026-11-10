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
using ProjectOdyssey.Screens.Import;
using ProjectOdyssey.Screens.Results;
using ProjectOdyssey.Screens.Settings;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens.ChartBrowser
{
    class ChartBrowserScreen : IGameScreen
    {
        private ChartBrowserView browserRenderer = null!;
        private ChartBrowserSession session = null!;

        private Vector2 lastMousePos;
        private InputHistory inputHistory;

        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }

        private Vector2 viewportDimensions;

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

            session = new ChartBrowserSession(charts, AudioManager);
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
                session.chartStackItems,
                session.FilteredChartSets,
                session.ChartSetCursor,
                session.ChartCursor,
                session.HoveredItem,
                session.scoreStackItems,
                session.CurrentScores);
        }

        public void Resize(Vector2 viewportDimensions)
        {
            this.viewportDimensions = viewportDimensions;
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
            string replayPath = session.HoveredScoreRecord.ReplayPath;

            ReplayData replayData;
            try
            {
                replayData = ReplayService.ReadReplayBinary(replayPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Could not read replay: {ex.Message}");
                return;
            }

            var (chartData, songPath) = session.FinaliseChartSelection();

            AudioManager.StopAudio();
            ScreenManager.Push(new ResultsScreen(replayData, chartData, inputHistory, Renderer, ScreenManager, AudioManager, SkinManager));
        }

        // Called when a screen above this one (gameplay -> results, or a replay's results) is popped.
        // A play may have saved a new score in the meantime, so reload the list.
        public void OnResume() => RefreshLibrary();

        public void RefreshLibrary()
        {
            session.ReloadCharts(ChartDatabase.GetChartsForBrowsing());
            session.RefreshScores();
        }

        public void OnKeyDown(Keys key, KeyModifiers modifiers)
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
                    CommitSelection(); break;
                case Keys.F2:
                    ScreenManager.Push(new ImportScreen(inputHistory, Renderer, ScreenManager, AudioManager, SkinManager)); break;
                case Keys.F5:
                    RefreshLibrary(); break;
                case Keys.O when modifiers.HasFlag(KeyModifiers.Control):
                    ScreenManager.Push(new SettingsScreen(Renderer, ScreenManager, AudioManager, SkinManager)); break;
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
            session.UpdateHover(ToLogical.ViewportToLogical(lastMousePos, viewportDimensions));
        }

        public void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (session.lastLogicalPos.X <= 1920 / 2)
            {
                // scroll scores
                session.MoveScores(-(int)e.OffsetY);
            }
            else
            {
                session.MoveSet(-(int)e.OffsetY);
            }
        }
    }
}