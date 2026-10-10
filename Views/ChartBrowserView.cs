using OpenTK.Mathematics;
using ProjectOdyssey.IO;
using ProjectOdyssey.Screens;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Render
{
    public class ChartBrowserView : IDisposable
    {
        // Renderer instance that this class will use to draw textures
        private Renderer renderer;

        private const int CardTextPadding = 16;
        private readonly MenuSkin skin;   // borrowed: owned and disposed by the SkinManager
        private Vector4 primaryTextColour = new Vector4(1f, 1f, 1f, 1f);

        // Either skin.MissingBackground (borrowed) or a chart's own background (owned by this renderer).
        private Texture background;
        private bool ownsBackground;
        private string? requestedBackgroundPath;

        public ChartBrowserView(MenuSkin skin, Renderer renderer)
        {
            this.skin = skin;
            this.renderer = renderer;
            background = skin.MissingBackground;
        }

        // Chart backgrounds are user content loaded at runtime, so this can legitimately fail
        // (file deleted, corrupt image). Any failure falls back to the skin's missing-background
        // texture instead of throwing.
        public void LoadBackgroundTexture(string path)
        {
            if (path == requestedBackgroundPath) return;
            requestedBackgroundPath = path;

            Texture? loaded = null;

            if (File.Exists(path))
            {
                try
                {
                    loaded = Texture.FromFile(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Warning] Could not load background '{path}': {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[Warning] Background image not found at {path}, using fallback.");
            }

            // Only release the old texture once the new one is ready.
            ReleaseBackground();

            if (loaded != null)
            {
                background = loaded;
                ownsBackground = true;
            }
            else
            {
                background = skin.MissingBackground;
            }
        }

        private void ReleaseBackground()
        {
            if (ownsBackground) background.Dispose();
            ownsBackground = false;
        }

        // Releases only the chart background this renderer loaded itself; skin textures
        // belong to the SkinManager.
        public void Dispose()
        {
            ReleaseBackground();
            background = skin.MissingBackground;
        }

        // Converts from center-based coordinates to bottom-left origin coordinates and draws the texture
        private void DrawBottomLeftOrigin(Texture texture, CardRect rect)
        {
            renderer.Draw(texture, rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f, rect.Width, rect.Height);
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

            // Empty library (e.g. a fresh build folder with no chart_library.db contents)
            if (chartSets.Count == 0)
            {
                renderer.DrawText(skin.Glyphs, "No charts found", 50, 50, primaryTextColour);
                return;
            }

            // Draw the set cards
            for (int i = 0; i < setCardRects.Count; i++)
            {
                var (slot, rect) = setCardRects[i];
                bool isSelected = slot == 0;
                bool isHovered = i == hoveredSet;

                Texture tex = (isSelected || isHovered) ? skin.SetCardHover : skin.SetCard; // Select texture based on whether its being hovered over or not
                DrawBottomLeftOrigin(tex, rect); // Draw the set card

                var set = chartSets[chartSetCursor + slot];
                float textY = rect.Y + (rect.Height - 40) / 2f; // vertically centre a single 40px line

                // Draw the set title in text
                renderer.DrawText(skin.Glyphs, set[0].Title, rect.X + CardTextPadding, textY, primaryTextColour);
            }

            // Draw the chart cards
            var currentSet = chartSets[chartSetCursor];
            for (int i = 0; i < chartCardRects.Count; i++)
            {
                var (chartIndex, rect) = chartCardRects[i];
                bool isSelected = chartIndex == chartCursor;
                bool isHovered = i == hoveredChart;

                Texture tex = (isSelected || isHovered) ? skin.ChartCardHover : skin.ChartCard; // Select texture based on whether its being hovered over or not
                DrawBottomLeftOrigin(tex, rect); // Draw the chart card

                var chart = currentSet[chartIndex];
                float textY = rect.Y + (rect.Height - 40) / 2f;

                // Draw the chart difficulty name in text
                renderer.DrawText(skin.Glyphs, chart.DiffName, rect.X + CardTextPadding, textY, primaryTextColour);

                // Draw the key count in text
                string keyText = chart.KeyCount + "K";
                float keyTextWidth = skin.Glyphs.MeasureText(keyText);
                renderer.DrawText(skin.Glyphs, keyText, rect.X + rect.Width - CardTextPadding - keyTextWidth, textY, primaryTextColour);
            }
        }
    }
}
