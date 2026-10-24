using FreeTypeSharp;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_LOAD;

namespace ProjectOdyssey.Render
{
    // One font at one pixel size, rasterised to per-character GL textures.
    // Owns its glyph textures (disposing the set deletes them). Created by the
    // SkinManager; renderers only borrow it.
    public sealed class GlyphSet : IDisposable
    {
        private const string SupportedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz 0123456789!?.,:%-+&/()";

        private readonly Dictionary<char, FreeTypeGlyph> glyphs;
        private bool disposed;

        public int FontSize { get; }

        // Tallest bearing in the set; aligns every glyph to a shared baseline.
        // Lives here (not on FontRenderer) so sets of different sizes don't clobber each other.
        public int Baseline { get; }

        private GlyphSet(int fontSize, Dictionary<char, FreeTypeGlyph> glyphs)
        {
            FontSize = fontSize;
            this.glyphs = glyphs;

            int baseline = 0;
            foreach (var g in glyphs.Values)
            {
                if (g.BearingY > baseline) baseline = g.BearingY;
            }
            Baseline = baseline;
        }

        public bool TryGetGlyph(char c, out FreeTypeGlyph glyph) => glyphs.TryGetValue(c, out glyph!);

        // Sums glyph advances to get a string's rendered width (for centring / right-aligning).
        public float MeasureText(string text)
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

        // Throws if the font can't be opened. FreeType handles are always released,
        // and any glyph textures already created are deleted if loading fails partway.
        public static unsafe GlyphSet Load(string fontPath, int fontSize)
        {
            if (!File.Exists(fontPath))
                throw new FileNotFoundException("Font file not found.", fontPath);

            var glyphs = new Dictionary<char, FreeTypeGlyph>();
            FT_LibraryRec_* lib = null;
            FT_FaceRec_* face = null;
            bool success = false;

            try
            {
                if ((int)FT_Init_FreeType(&lib) != 0)
                    throw new InvalidOperationException("FreeType failed to initialise.");

                int faceError;
                nint fontPathPtr = Marshal.StringToHGlobalAnsi(fontPath);
                try
                {
                    faceError = (int)FT_New_Face(lib, (byte*)fontPathPtr, 0, &face);
                }
                finally
                {
                    Marshal.FreeHGlobal(fontPathPtr);
                }

                if (faceError != 0)
                    throw new InvalidOperationException($"FreeType could not open '{fontPath}' (error {faceError}).");

                if ((int)FT_Set_Pixel_Sizes(face, 0, (uint)fontSize) != 0)
                    throw new InvalidOperationException($"FreeType could not set pixel size {fontSize}.");

                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

                foreach (char c in SupportedChars)
                {
                    if ((int)FT_Load_Char(face, c, FT_LOAD_RENDER) != 0) continue; // skip glyphs the font can't render

                    int texHandle = GL.GenTexture();
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

                success = true;
                return new GlyphSet(fontSize, glyphs);
            }
            finally
            {
                if (face != null) FT_Done_Face(face);
                if (lib != null) FT_Done_FreeType(lib);

                if (!success)
                {
                    foreach (var g in glyphs.Values) g.Dispose();
                }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            foreach (var g in glyphs.Values) g.Dispose();
            glyphs.Clear();
        }
    }
}
