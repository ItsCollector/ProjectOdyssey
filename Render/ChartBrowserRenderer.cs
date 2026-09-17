using ProjectOdyssey.IO;
using OpenTK.Mathematics;

namespace ProjectOdyssey.Render
{
    public class ChartBrowserRenderer : Renderer
    {
        private FontRenderer fontRenderer = new FontRenderer();
        private Dictionary<char, FreeTypeGlyph> glyphs_40;
        private Vector4 primaryTextColour = new Vector4(1f, 1f, 1f, 1f);

        public ChartBrowserRenderer()
        {
            glyphs_40 = fontRenderer.LoadGlyphs(40);
        }

        public void Initialise()
        {
            fontRenderer.Intitialise();
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);       // Renderer's own fixed 1920x1080 logic — real w/h ignored internally
            fontRenderer.Resize(1920, 1080);   // force the same fixed logical space, ignore real window size too
        }

        public void Draw(List<ChartBrowserRow> charts)
        {
            fontRenderer.Draw(glyphs_40, "example text", 200, 200, primaryTextColour);
        }

        public void Dispose()
        {
            fontRenderer.Dispose();

            foreach (var glyph in glyphs_40.Values)
            {
                glyph.Dispose();
            }
        }
    }
}