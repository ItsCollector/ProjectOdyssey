using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey
{
    public class GameplayScreen : IGameScreen
    {
        private GameSession session;
        private GameplayRenderer gameplayRenderer;

        public GameplayScreen()
        {
            session = new GameSession(new InputHistory());
            gameplayRenderer = new GameplayRenderer();
            gameplayRenderer.Intitialise();
            session.Start();
        }

        public void Initalise()
        {
            // Load chart and UI assets later 
            gameplayRenderer.Intitialise();
            session.Start();
        }

        public void Render()
        {
            gameplayRenderer.Draw(null, 200, 200, 100, 50);
        }

        public void Dispose()
        {
            session.Stop();
            gameplayRenderer.Dispose();
        }
    }
}
