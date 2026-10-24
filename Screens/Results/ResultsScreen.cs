using ProjectOdyssey.Audio;
using ProjectOdyssey.Engine.Scoring;
using ProjectOdyssey.Render;
using ProjectOdyssey.Skinning;
using ProjectOdyssey.Views;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.CompilerServices;
using ProjectOdyssey.IO;

namespace ProjectOdyssey.Screens
{
    public class ResultsScreen : IGameScreen
    {
        private ResultsView resultsView;
        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }
        private ChartData chartData;
        private ReplayData replayData;
        private ScoreData scoreData;

        public ResultsScreen(ReplayData replayData, ChartData chartData, Renderer renderer, ScreenManager screenManager, AudioManager audioManager, SkinManager skinManager)
        {
            scoreData = replayData.ScoreData;
            this.replayData = replayData;
            this.chartData = chartData;
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

        public void OnKeyDown(Keys key)
        {
            if (key == Keys.Escape)
            {
                ScreenManager.Pop();
            }

            if (key == Keys.F2)
            {
                ScreenManager.Replace(new ReplayScreen(chartData, replayData, chartData.SongFilePath, Renderer, ScreenManager, AudioManager, SkinManager));
            }
        }

        public void Load() { }
        public void Update(float deltaMs) { }
        public void Unload() { }
    }
}
