using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Screens
{
    public struct CardRect
    {
        public float X, Y, Width, Height;
        public bool Contains(float px, float py) =>
            px >= X && px < X + Width && py >= Y && py < Y + Height;
    }
}
