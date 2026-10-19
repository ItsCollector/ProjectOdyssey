using OpenTK.Mathematics;
using ProjectOdyssey.IO;
using ProjectOdyssey.Screens;
using ProjectOdyssey.Skinning;
using StbImageSharp;

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
        private string? loadedBgPath;      // what 'background' is currently showing
        private string? requestedBgPath;   // what the most recent request asked for

        // Background images are big, so they're decoded on a worker thread. OpenGL calls only work on the thread
        // that owns the GL context, so the worker only produces pixels. The texture itself is created on the
        // main thread, in UploadDecodedBackground().
        private sealed record DecodedBackground(int RequestId, string Path, ImageResult? Image);
        private DecodedBackground? decoded;   // handed from the worker to the main thread
        private int latestRequestId;          // a result is only used if it belongs to the most recent request

        // Card rects
        private CardRect backgroundRect = new CardRect { X = 0, Y = 0, Width = 1920, Height = 1080 };
        private CardRect chartInfoRect = new CardRect { X = 0, Y = 0, Width = 750, Height = 350 };
        private CardRect scorePanelRect = new CardRect { X = 0, Y = 450, Width = 650, Height = 100 };

        public ChartBrowserView(MenuSkin skin, Renderer renderer)
        {
            this.skin = skin;
            this.renderer = renderer;
            background = skin.MissingBackground;
        }

        // Call from the main thread whenever the selected chart changes. Cheap: the decoding happens on a worker.
        public void RequestBackgroundTexture(string path)
        {
            if (path == requestedBgPath) return;   // already showing, or already loading, exactly this

            requestedBgPath = path;
            int requestId = Interlocked.Increment(ref latestRequestId);   // supersedes anything still decoding

            if (path == loadedBgPath) return;      // going back to what's on screen: the bump above cancels the stale load

            Task.Run(() =>
            {
                if (requestId != Volatile.Read(ref latestRequestId)) return;

                ImageResult? image = DecodeImage(path);   // CPU only: no GL calls on this thread

                // If the selection moved on while we were decoding, drop the result
                if (requestId == Volatile.Read(ref latestRequestId))
                {
                    Interlocked.Exchange(ref decoded, new DecodedBackground(requestId, path, image));
                }
            });
        }

        // Chart backgrounds are user content loaded at runtime, so this can legitimately fail
        // (file deleted, corrupt image). A failure returns null, and the skin's missing-background is shown instead.
        private static ImageResult? DecodeImage(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"[Warning] Background image not found at {path}, using fallback.");
                return null;
            }

            try
            {
                return Texture.DecodeFile(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Could not load background '{path}': {ex.Message}");
                return null;
            }
        }

        // Main thread only (it makes GL calls). Picks up a finished decode, if there is one, and turns it into a texture.
        private void UploadDecodedBackground()
        {
            DecodedBackground? ready = Interlocked.Exchange(ref decoded, null);

            if (ready == null || ready.RequestId != Volatile.Read(ref latestRequestId)) return;

            ReleaseBackground();   // only now that the replacement is ready

            if (ready.Image != null)
            {
                background = Texture.FromImage(ready.Image, ready.Path);
                ownsBackground = true;
            }
            else
            {
                background = skin.MissingBackground;
            }

            loadedBgPath = ready.Path;   // also set on failure, so a broken image isn't retried every time
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
            Interlocked.Increment(ref latestRequestId);   // any decode still running is now stale and will be dropped
            Interlocked.Exchange(ref decoded, null);

            ReleaseBackground();
            background = skin.MissingBackground;
        }

        // Converts from center-based coordinates to bottom-left origin coordinates and draws the texture
        private void DrawTopLeftOrigin(Texture texture, CardRect rect)
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
            UploadDecodedBackground();

            DrawTopLeftOrigin(background, backgroundRect); // Draw background

            // Empty library (e.g. a fresh build folder with no chart_library.db contents)
            if (chartSets.Count == 0)
            {
                renderer.DrawText(skin.Glyphs, "No charts found", 50, 50, primaryTextColour);
                return;
            }

            DrawTopLeftOrigin(skin.ChartInfo, chartInfoRect); // Chart info panel
            DrawTopLeftOrigin(skin.ScorePanel, scorePanelRect); // Score panel

            var selectedSet = chartSets[chartSetCursor];
            var selectedChart = selectedSet[chartCursor];

            renderer.DrawText(skin.Glyphs, selectedChart.Title, chartInfoRect.X + CardTextPadding, chartInfoRect.Y + 20, primaryTextColour);
            renderer.DrawText(skin.Glyphs, selectedChart.DiffName, chartInfoRect.X + CardTextPadding, chartInfoRect.Y + 70, primaryTextColour);
            renderer.DrawText(skin.Glyphs, "View Score", scorePanelRect.X + CardTextPadding, scorePanelRect.Y + 30, primaryTextColour);

            // Draw the set cards
            for (int i = 0; i < setCardRects.Count; i++)
            {
                (int slot, CardRect rect) = setCardRects[i];
                bool isSelected = slot == 0;
                bool isHovered = i == hoveredSet;

                Texture tex;
                if (isSelected || isHovered) // Select texture based on whether its being hovered over or not
                {
                    tex = skin.SetCardHover;
                }
                else
                {
                    tex = skin.SetCard;
                }

                DrawTopLeftOrigin(tex, rect); // Draw the set card

                List<ChartBrowserRow> chartSet = chartSets[chartSetCursor + slot];
                float textY = rect.Y + (rect.Height - 40) / 2f; // vertically centre a single 40px line

                // Draw the set title in text
                renderer.DrawText(skin.Glyphs, chartSet[0].Title, rect.X + CardTextPadding, textY, primaryTextColour);
            }

            // Draw the chart cards
            var currentSet = chartSets[chartSetCursor];
            for (int i = 0; i < chartCardRects.Count; i++)
            {
                var (chartIndex, rect) = chartCardRects[i];
                bool isSelected = chartIndex == chartCursor;
                bool isHovered = i == hoveredChart;

                Texture tex;
                if (isSelected || isHovered) // Select texture based on whether its being hovered over or not
                {
                    tex = skin.ChartCardHover;
                }
                else
                {
                    tex = skin.ChartCard;
                }

                DrawTopLeftOrigin(tex, rect); // Draw the chart card

                ChartBrowserRow chart = currentSet[chartIndex];
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