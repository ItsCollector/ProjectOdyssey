using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.App;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Data.Replays;
using ProjectOdyssey.Engine.Chart;
using ProjectOdyssey.Engine.Scoring;
using ProjectOdyssey.Input;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.Screens.Components;
using ProjectOdyssey.Screens.Gameplay;
using ProjectOdyssey.Screens.Replay;
using ProjectOdyssey.Skinning;
using System.Collections.Generic;

namespace ProjectOdyssey.Screens.Results
{
    public class ResultsScreen : IGameScreen
    {
        private ResultsView resultsView;
        private readonly InputHistory inputHistory;
        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }
        private ChartData chartData;
        private ReplayData replayData;
        private ScoreData scoreData;

        private Vector2 lastMousePos;
        private int HoveredButton = -1;

        private int windowWidth = 1920;
        private int windowHeight = 1080;

        public List<(int Slot, Rect Rect)> ButtonRects { get; } = new()
        {
            (0, Rect.FromCenter(1700, 695, 320, 90)), // Retry button
            (1, Rect.FromCenter(1700, 815, 320, 90)), // Watch Replay button
            (2, Rect.FromCenter(1700, 935, 320, 90))  // Back button
        };

        private readonly List<Button> buttons;

        public ResultsScreen(ReplayData replayData, ChartData chartData, InputHistory inputHistory, Renderer renderer, ScreenManager screenManager, AudioManager audioManager, SkinManager skinManager)
        {
            scoreData = replayData.ScoreData;
            this.replayData = replayData;
            this.chartData = chartData;
            this.inputHistory = inputHistory;
            Renderer = renderer;
            ScreenManager = screenManager;
            AudioManager = audioManager;
            SkinManager = skinManager;
            resultsView = new ResultsView(SkinManager.MenuSkin, Renderer);

            buttons = new List<Button>
            {
                new Button(1700, 695, 320, 90, RetryChart), // Retry button
                new Button(1700, 815, 320, 90, WatchReplay), // Watch Replay button
                new Button(1700, 935, 320, 90, ExitToChartBrowser) // Back button
            };
        }

        public void Render()
        {
            resultsView.DrawResultsScreen(scoreData);
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button != MouseButton.Left) return;

            foreach (Button button in buttons)
            {
                if (button.TryClick()) return;
            }
        }

        public void OnMouseMove(MouseMoveEventArgs e)
        {
            Vector2 logicalPos = ToLogical(e.Position);

            foreach (Button button in buttons)
            {
                button.UpdateHoverState(logicalPos.X, logicalPos.Y);
            }
        }

        private Vector2 ToLogical(Vector2 windowPos)
        {
            return new Vector2(windowPos.X * 1920f / windowWidth, windowPos.Y * 1080f / windowHeight);
        }

        public void OnKeyDown(Keys key)
        {
            if (key == Keys.Escape)
            {
                ScreenManager.Pop();
            }
        }

        public void Resize(int width, int height)
        {
            windowWidth = width;
            windowHeight = height;
        }
        
        public void RetryChart()
        {
            ScreenManager.Replace(new GameplayScreen(chartData, chartData.SongFilePath, inputHistory, Renderer, ScreenManager, AudioManager, SkinManager));
        }

        public void WatchReplay()
        {
            ScreenManager.Replace(new ReplayScreen(chartData, replayData, chartData.SongFilePath, inputHistory, Renderer, ScreenManager, AudioManager, SkinManager));
        }

        public void ExitToChartBrowser()
        {
            ScreenManager.Pop();
        }

        public void Load() { }
        public void Update(float deltaMs) { }
        public void Unload() { }
    }
}
