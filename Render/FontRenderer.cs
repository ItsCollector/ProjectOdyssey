using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace ProjectOdyssey.Render
{
    // Draws text using a GlyphSet. Owns only its own GL objects (shader + quad mesh);
    // the glyph textures belong to the GlyphSet / SkinManager.
    public class FontRenderer : IDisposable
    {
        private int vao;
        private int vbo;
        private int ebo;
        private Shader shader;
        private Matrix4 projection;
        private bool disposed;

        public FontRenderer()
        {
            SetupMesh();
            shader = new Shader("Render/Shaders/shader.vert", "Render/Shaders/shader.frag");
        }

        // Creates a single quad mesh used for rendering all glyphs
        // UVs are arranged to match OpenGL coordinate space (bottom-left origin)
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

            // Vertex position attribute
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // UV attribute
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
        }

        // Renders a string using a preloaded glyph set
        public void Draw(GlyphSet glyphs, string text, float x, float y, Vector4 colour)
        {
            foreach (char c in text)
            {
                if (!glyphs.TryGetGlyph(c, out FreeTypeGlyph glyph))
                {
                    continue;
                }

                if (glyph.Width > 0 && glyph.Height > 0) // spaces have no bitmap, only an advance
                {
                    float xPos = x + glyph.BearingX;
                    float yPos = y + (glyphs.Baseline - glyph.BearingY);

                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, glyph.TextureHandle);

                    shader.SetInt("uUseTexture", 2); // texture * uColor branch
                    shader.SetVector2("uPosition", xPos + glyph.Width / 2f, yPos + glyph.Height / 2f);
                    shader.SetVector2("uSize", glyph.Width, glyph.Height);
                    shader.SetVector4("uColor", colour);

                    GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
                }

                x += glyph.Advance;
            }
        }

        public void Intitialise()
        {
            shader.Use();
            GL.BindVertexArray(vao);

            // Enable alpha blending for text transparency
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            shader.SetMatrix4("projection", projection);

            GL.ActiveTexture(TextureUnit.Texture0);
            shader.SetInt("uTexture", 0);
        }

        public void Resize(int width, int height)
        {
            projection = Matrix4.CreateOrthographicOffCenter(0f, width, height, 0f, -1f, 1f);
            shader.Use();
            shader.SetMatrix4("projection", projection);
        }

        public void Dispose()
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
