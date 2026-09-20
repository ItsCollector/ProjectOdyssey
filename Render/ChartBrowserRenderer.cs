using ProjectOdyssey.IO;
using OpenTK.Mathematics;

namespace ProjectOdyssey.Render
{
    public class ChartBrowserRenderer : Renderer
    {
        private FontRenderer fontRenderer = new FontRenderer();
        private Dictionary<char, FreeTypeGlyph> glyphs_40;
        private Vector4 primaryTextColour = new Vector4(1f, 1f, 1f, 1f);

        private Texture setCard;
        private Texture setCardHover;
        private Texture chartCard;
        private Texture chartCardHover;

        private string setCardPath = "Assets\\Chart Browser\\set_card.png";
        private string setCardHoverPath = "Assets\\Chart Browser\\set_card_hover.png";
        private string chartCardPath = "Assets\\Chart Browser\\chart_card.png";
        private string chartCardHoverPath = "Assets\\Chart Browser\\chart_card_hover.png";

        public ChartBrowserRenderer()
        {
            glyphs_40 = fontRenderer.LoadGlyphs(40);

            string baseDir = AppContext.BaseDirectory;
            setCard = LoadTexture(Path.Combine(baseDir, setCardPath));
            setCardHover = LoadTexture(Path.Combine(baseDir, setCardHoverPath));
            chartCard = LoadTexture(Path.Combine(baseDir, chartCardPath));
            chartCardHover = LoadTexture(Path.Combine(baseDir, chartCardHoverPath));
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

        public void DrawChartBrowser(List<ChartBrowserRow> charts)
        {
            //fontRenderer.Draw(glyphs_40, "example text", 200, 200, primaryTextColour);

            Draw(setCard, 320, 100);
            Draw(setCardHover, 320, 300);
            Draw(chartCard, 280, 500);
            Draw(chartCardHover, 280, 700);
        }

        public void Dispose()
        {
            fontRenderer.Dispose();

            foreach (var glyph in glyphs_40.Values)
            {
                glyph.Dispose();
            }

            base.Dispose();
        }
    }
}