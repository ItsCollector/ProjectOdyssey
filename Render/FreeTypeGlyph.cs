using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace ProjectOdyssey.Render
{
    public class FreeTypeGlyph : IDisposable
    {
        public int TextureHandle;
        public int Width;
        public int Height;
        public int BearingX;
        public int BearingY;
        public int Advance;

        public void Dispose()
        {
            if (TextureHandle != 0)
            {
                GL.DeleteTexture(TextureHandle);
                TextureHandle = 0;
            }
        }
    }
}
