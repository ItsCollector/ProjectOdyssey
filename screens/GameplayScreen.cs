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
            session = new GameSession(inputHistory);

            string skinDirectory = Path.Combine(AppContext.BaseDirectory, "skins/Skin 1");
            LoadSkin(skinDirectory);

            gameplayRenderer = new GameplayRenderer(skinConfig);
        }

        private void LoadSkin(string skinDirectory)
        {
            var filesResult = GameplaySkinParser.GetFiles(skinDirectory);
            if (!filesResult.isSuccess)
            {
                Console.WriteLine($"[Skin] {filesResult.error}");
                return;
            }

            var configResult = GameplaySkinParser.ParseSkinConfig(filesResult.value);
            if (!configResult.isSuccess)
            {
                Console.WriteLine($"[Skin] {configResult.error}");
                return;
            }

            var assetsResult = GameplaySkinParser.DiscoverAssets(filesResult.value);
            if (!assetsResult.isSuccess)
            {
                Console.WriteLine($"[Skin] {assetsResult.error}");
                return;
            }

            skinConfig = configResult.value;
            skinAssets = assetsResult.value;
        }

        public void Initalise()
        {
            gameplayRenderer.Intitialise();
            gameplayRenderer.LoadSkinTextures(skinAssets);
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