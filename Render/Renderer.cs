using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace ProjectOdyssey.Render
{
    public class Renderer : IDisposable
    {
        private int vao;
        private int vbo;
        private int ebo;
        private Shader shader;
        private Matrix4 projection;
        private int viewportWidth = 1920;
        private int viewportHeight = 1080;
        private bool disposed;

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

        // Every draw binds this renderer's own program + mesh. Uniform setters act on whichever
        // program is currently active, and several renderers (plus FontRenderer) now draw in the
        // same frame, so nothing can assume "my program is still bound from Initialise".
        private void BindForDraw()
        {
            shader.Use();
            GL.BindVertexArray(vao);
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        public void Draw(Texture? texture, float xPosition, float yPosition, float width = -1, float height = -1)
        {
            BindForDraw();

            if (texture == null)
            {
                shader.SetInt("uUseTexture", 0);
                shader.SetVector2("uPosition", xPosition, yPosition);
                shader.SetVector2("uSize", width, height);
            }
            else
            {
                float w = (width == -1) ? texture.Width : width;
                float h = (height == -1) ? texture.Height : height;

                GL.BindTexture(TextureTarget.Texture2D, texture.Handle);
                shader.SetInt("uUseTexture", 1);

                shader.SetVector2("uPosition", xPosition, yPosition);
                shader.SetVector2("uSize", w, h);
            }

            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        }

        public void DrawQuad(Texture? texture, float xPosition, float yPosition, float width = -1, float height = -1, Vector4? colour = null)
        {
            BindForDraw();

            if (texture == null)
            {
                shader.SetInt("uUseTexture", 0);
                shader.SetVector4("uColor", colour ?? Vector4.One);
                shader.SetVector2("uPosition", xPosition, yPosition);
                shader.SetVector2("uSize", width, height);
            }
            else
            {
                float w = (width == -1) ? texture.Width : width;
                float h = (height == -1) ? texture.Height : height;

                GL.BindTexture(TextureTarget.Texture2D, texture.Handle);
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

        public virtual void Resize(int width, int height)
        {
            projection = Matrix4.CreateOrthographicOffCenter(0f, width, height, 0f, -1f, 1f);

            // Upload to THIS renderer's program (the uniform call targets the active program)
            shader.Use();
            shader.SetMatrix4("projection", projection);
        }

        // Releases only the GL objects this renderer created. Skin textures and glyphs are
        // borrowed from the SkinManager and must NOT be disposed here (or in subclasses).
        public virtual void Dispose()
        {
            if (disposed) return;
            disposed = true;

            shader.Dispose();
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);   // was leaking
            GL.DeleteVertexArray(vao);
        }
    }
}
