namespace ProjectOdyssey.Screens.ChartBrowser
{
    public class ChartBrowserLayout
    {
        public const int SetCardWidth = 640, SetCardHeight = 120, SetCardSpacing = 8;
        public const int ChartCardWidth = 560, ChartCardHeight = 72, ChartCardSpacing = 6;
        public const int ChartCardIndent = 80;

        public const int ScoreCardWidth = 200, ScoreCardHeight = 72, ScoreCardSpacing = 6;

        // Flush against the right edge of the window
        public static float SetCardX(int windowWidth)
        {
            return windowWidth - SetCardWidth;
        }

        public static float ChartCardX(int windowWidth)
        {
            return windowWidth - SetCardWidth + ChartCardIndent;
        }
    }
}
