using OpenTK.Mathematics;

namespace ProjectOdyssey.Screens.ChartBrowser
{
    public class ChartBrowserLayout
    {
        public const int SetCardWidth = 640, SetCardHeight = 120, SetCardSpacing = 8;
        public const int ChartCardWidth = 560, ChartCardHeight = 72, ChartCardSpacing = 6, ChartCardIndent = 80;
        public const int ScoreCardWidth = 650, ScoreCardHeight = 100, ScoreCardSpacing = 6;

        public const float ScreenHeight = 1080;
        public const float ChartInfoHeight = 350;
        public const float ScoreListSpacing = 6f;

        public const int chartInfoTitleX = 24, chartInfoTitleY = 16;
        public const int chartInfoArtistX = 24, chartInfoArtistY = 62;

        public static float ScoreStackCenterY => ChartInfoHeight + ScoreListSpacing + ScoreCardHeight / 2f;



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
