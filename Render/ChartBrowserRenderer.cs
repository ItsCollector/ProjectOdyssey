using OpenTK.Mathematics;
using ProjectOdyssey.IO;
using ProjectOdyssey.Screens;
using System.Runtime.CompilerServices;

namespace ProjectOdyssey.Render
{
    public class ChartBrowserRenderer : Renderer
    {
        private FontRenderer fontRenderer = new FontRenderer();
        private Dictionary<char, FreeTypeGlyph> glyphs_40;
        private Vector4 primaryTextColour = new Vector4(1f, 1f, 1f, 1f);
        private const int CardTextPadding = 16;

        private Texture background;
        private Texture setCard;
        private Texture setCardHover;
        private Texture chartCard;
        private Texture chartCardHover;

        private string activeBackgroundPath = "Assets\\Chart Browser\\active_background_image.png";
        private string setCardPath = "Assets\\Chart Browser\\set_card.png";
        private string setCardHoverPath = "Assets\\Chart Browser\\set_card_hover.png";
        private string chartCardPath = "Assets\\Chart Browser\\chart_card.png";
        private string chartCardHoverPath = "Assets\\Chart Browser\\chart_card_hover.png";

        public ChartBrowserRenderer()
        {
            glyphs_40 = fontRenderer.LoadGlyphs(40);

            string baseDir = AppContext.BaseDirectory;
            background = LoadTexture(Path.Combine(baseDir, "Assets\\Chart Browser\\missing_background_image.png"));
            setCard = LoadTexture(Path.Combine(baseDir, setCardPath));
            setCardHover = LoadTexture(Path.Combine(baseDir, setCardHoverPath));
            chartCard = LoadTexture(Path.Combine(baseDir, chartCardPath));
            chartCardHover = LoadTexture(Path.Combine(baseDir, chartCardHoverPath));
        }

        public void LoadBackgroundTexture(string path)
        {
            if (path == activeBackgroundPath) return; 

            background.Dispose();

            if (!File.Exists(path))
            {
                Console.WriteLine($"[Warning] Background image not found at {path}, loading backup...");
                path = Path.Combine(AppContext.BaseDirectory, "Assets\\Chart Browser\\missing_background_image.png");
            }

            background = LoadTexture(path);
            activeBackgroundPath = path;
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
        private void DrawBottomLeftOrigin(Texture texture, CardRect rect)
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
            DrawBottomLeftOrigin(background, new CardRect { X = 0, Y = 0, Width = 1920, Height = 1080 }); // Draw background

            // Draw the set cards
            for (int i = 0; i < setCardRects.Count; i++)
            {
                var (slot, rect) = setCardRects[i];
                bool isSelected = slot == 0;
                bool isHovered = i == hoveredSet;

                Texture tex = (isSelected || isHovered) ? setCardHover : setCard; // Select texture based on whether its being hovered over or not
                DrawBottomLeftOrigin(tex, rect); // Draw the set card

                var set = chartSets[chartSetCursor + slot];
                float textY = rect.Y + (rect.Height - 40) / 2f; // vertically centre a single 40px line

                // Draw the set title in text
                fontRenderer.Draw(glyphs_40, set[0].Title, rect.X + CardTextPadding, textY, primaryTextColour);
            }

            // Draw the chart cards
            var currentSet = chartSets[chartSetCursor];
            for (int i = 0; i < chartCardRects.Count; i++)
            {
                var (chartIndex, rect) = chartCardRects[i];
                bool isSelected = chartIndex == chartCursor;
                bool isHovered = i == hoveredChart;

                Texture tex = (isSelected || isHovered) ? chartCardHover : chartCard; // Select texture based on whether its being hovered over or not
                DrawBottomLeftOrigin(tex, rect); // Draw the chart card

                var chart = currentSet[chartIndex];
                float textY = rect.Y + (rect.Height - 40) / 2f;

                // Draw the chart difficulty name in text
                fontRenderer.Draw(glyphs_40, chart.DiffName, rect.X + CardTextPadding, textY, primaryTextColour);

                // Draw the key count in text
                string keyText = chart.KeyCount + "K";
                float keyTextWidth = fontRenderer.MeasureText(glyphs_40, keyText);
                fontRenderer.Draw(glyphs_40, keyText, rect.X + rect.Width - CardTextPadding - keyTextWidth, textY, primaryTextColour);
            }
        }

        public override void Dispose()
        {
            fontRenderer.Dispose();

            foreach (var glyph in glyphs_40.Values)
            {
                glyph.Dispose();
            }

            background.Dispose();
            setCard.Dispose();
            setCardHover.Dispose();
            chartCard.Dispose();
            chartCardHover.Dispose();

            base.Dispose();
        }
    }
}
