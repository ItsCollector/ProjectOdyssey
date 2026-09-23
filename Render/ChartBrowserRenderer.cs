using OpenTK.Mathematics;
using ProjectOdyssey.IO;
using ProjectOdyssey.Screens;

namespace ProjectOdyssey.Render
{
    public class ChartBrowserRenderer : Renderer
    {
        private FontRenderer fontRenderer = new FontRenderer();
        private Dictionary<char, FreeTypeGlyph> glyphs_40;
        private Vector4 primaryTextColour = new Vector4(1f, 1f, 1f, 1f);
        private const int CardTextPadding = 16;

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
            base.Resize(width, height);
            fontRenderer.Resize(1920, 1080);
        }

        // Converts from center-based coordinates to bottom-left origin coordinates and draws the texture
        private void DrawCard(Texture texture, CardRect rect)
        {
            Draw(texture, rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f, rect.Width, rect.Height);
        }

        public void DrawChartBrowser(
            List<(int Slot, CardRect Rect)> setCardRects,
            List<(int ChartIndex, CardRect Rect)> chartCardRects,
            List<List<ChartBrowserRow>> chartSets,
            int chartSetCursor,
            int chartCursor,
            int hoveredSet,
            int hoveredChart)
        {
            for (int i = 0; i < setCardRects.Count; i++)
            {
                var (slot, rect) = setCardRects[i];
                bool isSelected = slot == 0;
                bool isHovered = i == hoveredSet;

                Texture tex = (isSelected || isHovered) ? setCardHover : setCard;
                DrawCard(tex, rect);

                var set = chartSets[chartSetCursor + slot];
                float textY = rect.Y + (rect.Height - 40) / 2f; // vertically centre a single 40px line
                fontRenderer.Draw(glyphs_40, set[0].Title, rect.X + CardTextPadding, textY, primaryTextColour);
            }

            var currentSet = chartSets[chartSetCursor];
            for (int i = 0; i < chartCardRects.Count; i++)
            {
                var (chartIndex, rect) = chartCardRects[i];
                bool isSelected = chartIndex == chartCursor;
                bool isHovered = i == hoveredChart;

                Texture tex = (isSelected || isHovered) ? chartCardHover : chartCard;
                DrawCard(tex, rect);

                var chart = currentSet[chartIndex];
                float textY = rect.Y + (rect.Height - 40) / 2f;

                fontRenderer.Draw(glyphs_40, chart.DiffName, rect.X + CardTextPadding, textY, primaryTextColour);

                string keyText = chart.KeyCount + "K";
                float keyTextWidth = fontRenderer.MeasureText(glyphs_40, keyText);
                fontRenderer.Draw(glyphs_40, keyText, rect.X + rect.Width - CardTextPadding - keyTextWidth, textY, primaryTextColour);
            }
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
