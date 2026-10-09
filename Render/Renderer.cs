using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace ProjectOdyssey.Render
{
    // The only class that owns GL drawing resources. Screens queue commands during Render();
    // MainWindow calls Flush() once per frame after every screen has queued.
    // Textures and glyph sets are borrowed from the SkinManager: a command only holds a raw
    // handle, so Flush must run before anything disposes the texture it refers to.
    public class Renderer : IDisposable
    {
        private static readonly string shaderVertPath = Path.Combine(AppContext.BaseDirectory, "Render", "Shaders", "shader.vert");
        private static readonly string shaderFragPath = Path.Combine(AppContext.BaseDirectory, "Render", "Shaders", "shader.frag");

        // Everything draws in a fixed logical space; GL.Viewport scales it to the window.
        private const float LogicalWidth = 1920f;
        private const float LogicalHeight = 1080f;

        private int vao;
        private int vbo;
        private int ebo;
        private int baseTexture;    // 1x1 white, so flat quads are just "white * colour"
        private readonly Shader shader;
        private static readonly Vector4 FullUv = new(0f, 0f, 1f, 1f);
        private readonly List<DrawCommand> queue = new(4096);
        private bool disposed;

        // Needs a live GL context (construct after the window has loaded).
        public Renderer()
        {
            SetupMesh();
            shader = new Shader(shaderVertPath, shaderFragPath);

            shader.Use();
            shader.SetMatrix4("projection", Matrix4.CreateOrthographicOffCenter(0f, LogicalWidth, LogicalHeight, 0f, -1f, 1f));
            shader.SetInt("uTexture", 0);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        // Unit quad centred on the origin, with UVs matching OpenGL's bottom-left origin
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

            // 1x1 white texture used for flat-colour quads
            baseTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, baseTexture);
            byte[] white = { 255, 255, 255, 255 };
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, white);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }

        private void Queue(int textureHandle, float x, float y, float w, float h, Vector4 colour, Vector4? uv = null)
        {
            queue.Add(new DrawCommand
            {
                TextureHandle = textureHandle,
                X = x,
                Y = y,
                Width = w,
                Height = h,
                Colour = colour,
                UvRect = uv ?? FullUv
            });
        }

        // Textured quad centred on (x, y). Width/height default to the texture's own size.
        // tint multiplies the texture; use alpha < 1 to fade it.
        public void Draw(Texture texture, float x, float y, float width = -1, float height = -1, Vector4? tint = null)
        {
            Queue(texture.Handle, x, y,
                  width == -1 ? texture.Width : width,
                  height == -1 ? texture.Height : height,
                  tint ?? Vector4.One);
        }

        // Flat-colour quad centred on (x, y)
        public void DrawQuad(float x, float y, float width, float height, Vector4 colour)
        {
            Queue(baseTexture, x, y, width, height, colour);
        }

        // Queues one command per glyph. (x, y) is the top-left of the text line.
        public void DrawText(GlyphSet glyphs, string text, float x, float y, Vector4 colour)
        {
            foreach (char c in text)
            {
                if (!glyphs.TryGetGlyph(c, out FreeTypeGlyph glyph))
                {
                    continue;
                }

                if (glyph.Width > 0 && glyph.Height > 0) // spaces have no bitmap, only an advance
                {
                    float left = x + glyph.BearingX;
                    float top = y + (glyphs.Baseline - glyph.BearingY);

                    Queue(glyph.TextureHandle,
                          left + glyph.Width / 2f,    // quads are centre-based
                          top + glyph.Height / 2f,
                          glyph.Width,
                          glyph.Height,
                          colour);
                }

                x += glyph.Advance;
            }
        }

        // Draws the part of the texture that sits at or above clipY (screen Y grows downward).
        public void DrawClippedBelow(Texture texture, float x, float y, float width, float height, float clipY)
        {
            float top = y - height / 2f;
            float visibleBottom = Math.Min(y + height / 2f, clipY);
            float visibleHeight = visibleBottom - top;

            if (visibleHeight <= 0f) return; // entirely clipped away

            float v1 = visibleHeight / height; // fraction of the texture that survives
            Queue(texture.Handle, 
                  x, 
                  top + visibleHeight / 2f, 
                  width, 
                  visibleHeight,
                  Vector4.One, 
                  new Vector4(0f, 0f, 1f, v1));
        }

        // Draws everything queued this frame, in the order it was queued, then empties the queue.
        // Call exactly once per frame, after all screens have queued.
        public void Flush()
        {
            if (queue.Count == 0) return;

            shader.Use();
            GL.BindVertexArray(vao);
            GL.ActiveTexture(TextureUnit.Texture0);

            int boundTexture = -1;
            foreach (ref readonly DrawCommand cmd in CollectionsMarshal.AsSpan(queue))
            {
                if (cmd.TextureHandle != boundTexture) // only rebind when the texture changes
                {
                    GL.BindTexture(TextureTarget.Texture2D, cmd.TextureHandle);
                    boundTexture = cmd.TextureHandle;
                }

                shader.SetVector2("uPosition", cmd.X, cmd.Y);
                shader.SetVector2("uSize", cmd.Width, cmd.Height);
                shader.SetVector4("uColor", cmd.Colour);
                shader.SetVector4("uUvRect", cmd.UvRect);

                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            }

            queue.Clear(); // keeps capacity: no reallocation next frame
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            shader.Dispose();
            GL.DeleteTexture(baseTexture);
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);
            GL.DeleteVertexArray(vao);
        }
    }
}