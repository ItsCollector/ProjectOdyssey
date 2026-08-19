using OpenTK.Graphics.OpenGL4;

namespace ProjectOdyssey
{
    public class Texture : IDisposable
    {
        public int handle;
        public int width;
        public int height;
        public string imgPath;
        
        public Texture(int handle, int width, int height, string imgPath)
        {
            this.handle = handle;
            this.width = width;
            this.height = height;
            this.imgPath = imgPath;
        }

        public void Dispose()
        {
            if (handle != 0)
            {
                GL.DeleteTexture(handle);
                handle = 0;
            }
        }
    }
}
