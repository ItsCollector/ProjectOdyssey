using ProjectOdyssey.Audio;
using ProjectOdyssey.Engine.Scoring;
using ProjectOdyssey.Render;
using ProjectOdyssey.Skinning;
using ProjectOdyssey.Views;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ProjectOdyssey.Screens
{
    public class ResultsScreen : IGameScreen
    {
        private ResultsView resultsView;
        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }
        private ScoreData scoreData;

        public ResultsScreen(ScoreData scoreData, Renderer renderer, ScreenManager screenManager, AudioManager audioManager, SkinManager skinManager)
        {
            this.scoreData = scoreData;
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
        }

        public void Load() { }
        public void Update(float deltaMs) { }
        public void Unload() { }
    }
}
