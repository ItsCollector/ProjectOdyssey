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

        public List<(int Slot, CardRect Rect)> ButtonRects { get; } = new()
        {
            (0, CardRect.FromCenter(1700, 695, 320, 90)), // Retry button
            (1, CardRect.FromCenter(1700, 815, 320, 90)), // Watch Replay button
            (2, CardRect.FromCenter(1700, 935, 320, 90))  // Back button
        };

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
        }

        public void Render()
        {
            resultsView.DrawResultsScreen(scoreData);
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button != MouseButton.Left || HoveredButton == -1) return;

            if (HoveredButton == 0) // Retry chart
            {
                ScreenManager.Replace(new GameplayScreen(chartData, chartData.SongFilePath, inputHistory, Renderer, ScreenManager, AudioManager, SkinManager));
                return;
            }
            else if (HoveredButton == 1) // Watch replay
            {
                ScreenManager.Replace(new ReplayScreen(chartData, replayData, chartData.SongFilePath, inputHistory, Renderer, ScreenManager, AudioManager, SkinManager));
                return;
            }
            else if (HoveredButton == 2) // Exit to chart browser
            {
                ScreenManager.Pop();
                return;
            }
        }

        public void OnMouseMove(MouseMoveEventArgs e)
        {
            lastMousePos = e.Position;
            UpdateHover(ToLogical(lastMousePos));
        }

        // LogicalPos must already be in the fixed 1920x1080 space.
        public void UpdateHover(Vector2 logicalPos)
        {
            int hovered = -1;

            for (int i = 0; i < ButtonRects.Count; i++)
            {
                if (ButtonRects[i].Rect.Contains(logicalPos.X, logicalPos.Y))
                {
                    hovered = ButtonRects[i].Slot;
                    break;
                }
            }

            if (hovered != HoveredButton)
            {
                HoveredButton = hovered;
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

        public void Load() { }
        public void Update(float deltaMs) { }
        public void Unload() { }
    }
}
