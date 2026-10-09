using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Render
{
    public struct DrawCommand
    {
        public int TextureHandle;
        public float X, Y;          // centre of the quad, in 1920x1080 logical space
        public float Width, Height;
        public Vector4 Colour;
        public Vector4 UvRect;      // (u0, v0, u1, v1); (0,0,1,1) = whole texture
    }
}
