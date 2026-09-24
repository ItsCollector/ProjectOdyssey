using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Screens
{
    public struct CardRect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        // Checks if a point (px, py) is inside the rectangle defined by this CardRect.
        public bool Contains(float px, float py)
        {
            bool insideHorizontally = px >= X && px < X + Width;
            bool insideVertically = py >= Y && py < Y + Height;

            return insideHorizontally && insideVertically;
        }
    }
}
