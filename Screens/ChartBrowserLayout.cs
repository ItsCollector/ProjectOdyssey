using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Screens
{
    public class ChartBrowserLayout
    {
        public const int SetCardWidth = 640, SetCardHeight = 120, SetCardSpacing = 8;
        public const int ChartCardWidth = 560, ChartCardHeight = 72, ChartCardSpacing = 6;
        public const int ChartCardIndent = 80; 

        // Flush against the right edge of the window
        public static float SetCardX(int windowWidth) => windowWidth - SetCardWidth;
        public static float ChartCardX(int windowWidth) => windowWidth - SetCardWidth + ChartCardIndent;
    }
}
