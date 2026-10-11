using ProjectOdyssey.Audio;
using ProjectOdyssey.Engine.Scoring;
using ProjectOdyssey.Render;
using ProjectOdyssey.Skinning;
using ProjectOdyssey.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public ResultsScreen(ScoreData scoreData)
        {
            this.scoreData = scoreData;
        }

        public void Load()
        {
            resultsView = new ResultsView(SkinManager.MenuSkin, Renderer);
        }

        public void Update(float deltaMs)
        {

        }

        public void Render()
        {
            resultsView.DrawResultsScreen(scoreData);
        }
        public void Unload()
        {
            // Cleanup resources if needed
        }

        public void OnKeyDown()
        {

        }
    }
}
