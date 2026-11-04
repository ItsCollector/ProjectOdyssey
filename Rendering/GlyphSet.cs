using FreeTypeSharp;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_LOAD;

namespace ProjectOdyssey.Rendering
{
    // One font, rasterised once at RasterSize to per-character GL textures. Text can then be drawn at any
    // size by scaling the quads, so no extra sets are needed per size.
    // Owns its glyph textures (disposing the set deletes them). Created by the
    // SkinManager; renderers only borrow it.
    public sealed class GlyphSet : IDisposable
    {
        private const string SupportedChars = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        private readonly Dictionary<char, FreeTypeGlyph> glyphs;
        private bool disposed;

        // The size text is drawn at when no size is given
        public int FontSize { get; }

        // The pixel size the glyph bitmaps were rendered at. Every glyph metric (Width, Height, BearingX/Y,
        // Advance, Baseline) is in these pixels; multiply by ScaleFor(size) to get them at a draw size.
        // Keep it >= the largest size you draw: text above it is a stretched bitmap and looks soft.
        public int RasterSize { get; }

        // Tallest bearing in the set (in RasterSize pixels); aligns every glyph to a shared baseline.
        public int Baseline { get; }

        private GlyphSet(int fontSize, int rasterSize, Dictionary<char, FreeTypeGlyph> glyphs)
        {
            FontSize = fontSize;
            RasterSize = rasterSize;
            this.glyphs = glyphs;

            int baseline = 0;
            foreach (var g in glyphs.Values)
            {
                if (g.BearingY > baseline) baseline = g.BearingY;
            }
            Baseline = baseline;
        }

        public float ScaleFor(float size) => size / RasterSize;

        public bool TryGetGlyph(char c, out FreeTypeGlyph glyph) => glyphs.TryGetValue(c, out glyph!);

        // Width at the default size
        public float MeasureText(string text) => MeasureText(text, FontSize);

        // Sums glyph advances to get a string's rendered width at the given draw size (for centring / right-aligning).
        public float MeasureText(string text, float size)
        {
            float width = 0f;
            foreach (char c in text)
            {
                if (glyphs.TryGetValue(c, out var glyph))
                {
                    width += glyph.Advance;
                }
            }
            return width * ScaleFor(size);
        }

        // Throws if the font can't be opened. FreeType handles are always released,
        // and any glyph textures already created are deleted if loading fails partway.
        // rasterSize <= 0 means "rasterise at fontSize" (the old behaviour).
        public static unsafe GlyphSet Load(string fontPath, int fontSize, int rasterSize = 0)
        {
            if (!File.Exists(fontPath))
                throw new FileNotFoundException("Font file not found.", fontPath);

            if (rasterSize <= 0) rasterSize = fontSize;

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

                if ((int)FT_Set_Pixel_Sizes(face, 0, (uint)rasterSize) != 0)
                    throw new InvalidOperationException($"FreeType could not set pixel size {rasterSize}.");

                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

                foreach (char c in SupportedChars)
                {
                    if ((int)FT_Load_Char(face, c, FT_LOAD_RENDER) != 0) continue; // skip glyphs the font can't render

                    int bitmapWidth = (int)face->glyph->bitmap.width;
                    int bitmapRows = (int)face->glyph->bitmap.rows;
                    bool hasPixels = bitmapWidth > 0 && bitmapRows > 0;   // the space glyph has an empty bitmap

                    int texHandle = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, texHandle);
                    GL.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        PixelInternalFormat.R8,
                        bitmapWidth,
                        bitmapRows,
                        0,
                        PixelFormat.Red,
                        PixelType.UnsignedByte,
                        (nint)face->glyph->bitmap.buffer
                    );

                    // Mipmaps keep text clean when it's drawn smaller than RasterSize
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)(hasPixels ? TextureMinFilter.LinearMipmapLinear : TextureMinFilter.Linear));
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                    int[] swizzle = { (int)All.Red, (int)All.Red, (int)All.Red, (int)All.Red };
                    GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleRgba, swizzle);

                    if (hasPixels)
                    {
                        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                    }

                    glyphs[c] = new FreeTypeGlyph
                    {
                        TextureHandle = texHandle,
                        Width = bitmapWidth,
                        Height = bitmapRows,
                        BearingX = face->glyph->bitmap_left,
                        BearingY = face->glyph->bitmap_top,
                        Advance = (int)(face->glyph->advance.x >> 6)
                    };
                }

                success = true;
                return new GlyphSet(fontSize, rasterSize, glyphs);
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