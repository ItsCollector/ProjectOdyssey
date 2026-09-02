using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

namespace ProjectOdyssey
{
    public class Renderer
    {
        private int vao;
        private int vbo;
        private int ebo;
        private Shader shader;
        private Matrix4 projection;
        private int viewportWidth = 1920;
        private int viewportHeight = 1080;

        public Renderer()
        {
            SetupMesh();
            shader = new Shader("Render/Shaders/shader.vert", "Render/Shaders/shader.frag");

            Resize(1920, 1080); 
        }

        private void SetupMesh()
        {
            float[] vertices =
            {
                -0.5f, -0.5f,  0.0f, 0.0f,
                 0.5f, -0.5f,  1.0f, 0.0f,
                 0.5f,  0.5f,  1.0f, 1.0f,
                -0.5f,  0.5f,  0.0f, 1.0f
            };

            uint[] indices =
            {
                0, 1, 3,
                1, 2, 3
            };

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            // Vertex Position attribute
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // UV attribute
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
        }

        public void Intitialise()
        {
            shader.Use();
            GL.BindVertexArray(vao);

            shader.SetMatrix4("projection", projection);
            shader.SetInt("uTexture", 0); 
            shader.SetInt("uUseTexture", 0);
            shader.SetVector4("uColor", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

            GL.ActiveTexture(TextureUnit.Texture0);
        }

        // Creates Texture object based on image file at given path
        public Texture LoadTexture(string path)
        {
            int texHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texHandle);

            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                image.Width,
                image.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                image.Data
            );

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            return new Texture(texHandle, image.Width, image.Height, path);
        }

        // Loads multiple textures from a list of paths, returning a list of Texture objects
        public List<Texture> LoadTextures(string[] paths)
        {
            List<Texture> textures = new List<Texture>();

            foreach (string path in paths)
            {
                int texHandle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texHandle);

                using var stream = File.OpenRead(path);
                var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    image.Width,
                    image.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    image.Data
                );

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                textures.Add(new Texture(texHandle, image.Width, image.Height, path));
            }

            return textures;
        }

        public void Draw(Texture? texture, float xPosition, float yPosition, float width = -1, float height = -1)
        {
            if (texture == null)
            {
                shader.SetInt("uUseTexture", 0);
                shader.SetVector2("uPosition", xPosition, yPosition);
                shader.SetVector2("uSize", width, height);
            }
            else
            {
                float w = (width == -1) ? texture.width : width;
                float h = (height == -1) ? texture.height : height;

                GL.BindTexture(TextureTarget.Texture2D, texture.handle);
                shader.SetInt("uUseTexture", 1);

                shader.SetVector2("uPosition", xPosition, yPosition);
                shader.SetVector2("uSize", w, h);
            }

            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        }

        public void DrawClippedBelow(Texture texture, float x, float y, float width, float height, float clipBelowScreenY)
        {
            GL.Enable(EnableCap.ScissorTest);

            const float logicalWidth = 1920f;
            const float logicalHeight = 1080f;

            float scaleX = viewportWidth / logicalWidth;
            float scaleY = viewportHeight / logicalHeight;

            int scissorBottomY = (int)Math.Max(0, viewportHeight - (clipBelowScreenY * scaleY));
            int scissorHeight = Math.Max(0, viewportHeight - scissorBottomY);
            int scissorX = (int)((x - width / 2) * scaleX);
            int scissorWidth = Math.Max(0, (int)(width * scaleX) + 1);

            GL.Scissor(scissorX, scissorBottomY, scissorWidth, scissorHeight);

            Draw(texture, x, y, width, height);

            GL.Disable(EnableCap.ScissorTest);
        }

        public void UpdateViewportSize(int width, int height)
        {
            viewportWidth = width;
            viewportHeight = height;
        }

        public void Resize(int width, int height)
        {
            projection = Matrix4.CreateOrthographicOffCenter(0f, width, height, 0f, -1f, 1f);
        }

        public void Dispose()
        {
            shader.Dispose();
            GL.DeleteBuffer(vbo);
            GL.DeleteVertexArray(vao);
        }
    }
}
