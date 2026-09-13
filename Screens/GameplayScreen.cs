using ProjectOdyssey.Engine;
using ProjectOdyssey.Input;
using ProjectOdyssey.IO;
using ProjectOdyssey.Render;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens
{
    public class GameplayScreen : IGameScreen
    {
        private readonly ChartData chartData;
        private readonly InputHistory inputHistory;

        // Assigned in Load(), which the ScreenManager guarantees runs before
        // Update()/Render()/Resize()/Unload() are ever called on this screen.
        private GameSession session = null!;
        private GameplayRenderer gameplayRenderer = null!;
        private GameplaySkinConfig skinConfig = null!;
        private SkinAssets skinAssets = null!;

        public GameplayScreen(ChartData chartData, InputHistory inputHistory)
        {
            this.chartData = chartData;
            this.inputHistory = inputHistory;
        }

        public void Load()
        {
            (skinConfig, skinAssets) = GameplaySkinParser.LoadSkin().value;
            session = new GameSession(inputHistory, chartData);
            gameplayRenderer = new GameplayRenderer(skinConfig, skinAssets);
            gameplayRenderer.Intitialise();
            session.Start(chartData);
        }

        public void Update(float deltaMs)
        {
            // GameSession deliberately ticks itself on its own background
            // thread at a fixed 1000Hz so hit-timing stays independent of
            // render framerate. There's nothing to drive from here yet -
            // this is an explicit no-op rather than a missing implementation.
            // If that ever changes (e.g. session becomes frame-driven),
            // this is where session.Tick(deltaMs) would go.
        }

        public void Render()
        {
            gameplayRenderer.DrawGameplay(session.notesByColumn, session.columnCursors);
        }

        public void Resize(int width, int height)
        {
            gameplayRenderer.UpdateViewportSize(width, height);
        }

        public void Unload()
        {
            session.Stop();
            gameplayRenderer.Dispose();
        }
    }
}
