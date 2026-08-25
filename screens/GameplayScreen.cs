namespace ProjectOdyssey
{
    public class GameplayScreen : IGameScreen
    {
        private GameSession session;
        private GameplayRenderer gameplayRenderer;
        private ChartData chartData;
        private GameplaySkinConfig skinConfig;
        private SkinAssets skinAssets;

        public GameplayScreen(ChartData chartData, InputHistory inputHistory)
        {
            this.chartData = chartData;
            (skinConfig, skinAssets) = GameplaySkinParser.LoadSkin().value;
            session = new GameSession(inputHistory, chartData);
            gameplayRenderer = new GameplayRenderer(skinConfig, skinAssets);
            gameplayRenderer.Intitialise();
            session.Start(chartData);
        }

        public void Render()
        {
            gameplayRenderer.DrawGameplay(session.notesByColumn, session.columnCursors);
        }

        public void Dispose()
        {
            session.Stop();
            gameplayRenderer.Dispose();
        }
    }
}