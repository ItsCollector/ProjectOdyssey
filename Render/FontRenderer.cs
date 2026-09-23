using FreeTypeSharp;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_LOAD;
using static FreeTypeSharp.FT_Render_Mode_;

namespace ProjectOdyssey.Render
{
    public class FontRenderer
    {
        string fontPath = "Assets/Fonts/Exo2.ttf";

        private int texHandle;
        private int vao;
        private int vbo;
        private int ebo;
        private Shader shader;
        private Matrix4 projection;

        // Used to align all glyphs to a shared baseline across different characters
        private int baseline;

        public unsafe FontRenderer()
        {
            SetupMesh();
            shader = new Shader("Render/Shaders/shader.vert", "Render/Shaders/shader.frag");
        }

        public unsafe Dictionary<char, FreeTypeGlyph> LoadGlyphs(int fontSize)
        {
            Dictionary<char, FreeTypeGlyph> glyphs = new Dictionary<char, FreeTypeGlyph>();

            // Initialise FreeType library
            FT_LibraryRec_* lib;
            FT_Init_FreeType(&lib);

            // Allocate unmanaged memory for the font path
            nint fontPathPtr = Marshal.StringToHGlobalAnsi(fontPath);

            FT_FaceRec_* face;

            try
            {
                // Load font face
                FT_New_Face(lib, (byte*)fontPathPtr, 0, &face);
            }
            finally
            {
                // Ensure unmanaged memory is always freed
                Marshal.FreeHGlobal(fontPathPtr);
            }

            // Set glyph size
            FT_Set_Pixel_Sizes(face, 0, (uint)fontSize);

            string supportedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz 0123456789!?.,:%-+&/()";

            foreach (char c in supportedChars)
            {
                FT_Load_Char(face, c, FT_LOAD_RENDER);

                int texHandle = GL.GenTexture();

                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

                GL.BindTexture(TextureTarget.Texture2D, texHandle);
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.R8,
                    (int)face->glyph->bitmap.width,
                    (int)face->glyph->bitmap.rows,
                    0,
                    PixelFormat.Red,
                    PixelType.UnsignedByte,
                    (nint)face->glyph->bitmap.buffer
                );

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                int[] swizzle = { (int)All.Red, (int)All.Red, (int)All.Red, (int)All.Red };
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleRgba, swizzle);

                glyphs[c] = new FreeTypeGlyph
                {
                    TextureHandle = texHandle,
                    Width = (int)face->glyph->bitmap.width,
                    Height = (int)face->glyph->bitmap.rows,
                    BearingX = face->glyph->bitmap_left,
                    BearingY = face->glyph->bitmap_top,
                    Advance = (int)(face->glyph->advance.x >> 6)
                };
            }

            // Compute maximum bearing to establish consistent text baseline
            foreach (var g in glyphs.Values)
            {
                if (g.BearingY > baseline)
                {
                    baseline = g.BearingY;
                }
            }

            // Clean up FreeType resources
            FT_Done_Face(face);
            FT_Done_FreeType(lib);

            return glyphs;
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

        // Sums glyph advances to get a string's rendered width, for right-aligning
        // or centring text (e.g. a key count against a card's right edge).
        public float MeasureText(Dictionary<char, FreeTypeGlyph> glyphs, string text)
        {
            float width = 0f;
            foreach (char c in text)
            {
                if (glyphs.TryGetValue(c, out var glyph))
                {
                    width += glyph.Advance;
                }
            }
            return width;
        }

        // Renders a string using preloaded glyph textures
        public void Draw(Dictionary<char, FreeTypeGlyph> glyphs, string text, float x, float y, Vector4 colour)
        {
            foreach (char c in text)
            {
                if (!glyphs.TryGetValue(c, out FreeTypeGlyph glyph))
                {
                    continue;
                }

                float xPos = x + glyph.BearingX;
                float yPos = y + (baseline - glyph.BearingY);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, glyph.TextureHandle);

                shader.SetInt("uUseTexture", 2); // texture * uColor branch
                shader.SetVector2("uPosition", xPos + glyph.Width / 2f, yPos + glyph.Height / 2f);
                shader.SetVector2("uSize", glyph.Width, glyph.Height);
                shader.SetVector4("uColor", colour); 

                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

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

        public void SetVector3(string name, Vector3 vector)
        {
            int location = GL.GetUniformLocation(texHandle, name);
            GL.Uniform3(location, vector);
        }

        public void Resize(int width, int height)
        {
            projection = Matrix4.CreateOrthographicOffCenter(0f, width, height, 0f, -1f, 1f);
            shader.Use();
            shader.SetMatrix4("projection", projection);
        }

        public void Dispose()
        {
            shader.Dispose();
            GL.DeleteBuffer(vbo);
            GL.DeleteVertexArray(vao);
        }
    }
}