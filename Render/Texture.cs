using OpenTK.Graphics.OpenGL4;

namespace ProjectOdyssey.Render
{
    public class Texture : IDisposable
    {
        public int Handle;
        public int Width;
        public int Height;
        public string ImgPath;

        public Texture(int handle, int width, int height, string imgPath)
        {
            this.Handle = handle;
            this.Width = width;
            this.Height = height;
            this.ImgPath = imgPath;
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                GL.DeleteTexture(Handle);
                Handle = 0;
            }
        }
    }
}
