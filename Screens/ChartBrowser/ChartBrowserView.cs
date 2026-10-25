using OpenTK.Mathematics;
using ProjectOdyssey.Data.Charts;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.Screens.Components;
using ProjectOdyssey.Skinning;
using StbImageSharp;

namespace ProjectOdyssey.Screens.ChartBrowser
{
    public class ChartBrowserView : IDisposable
    {
        // Renderer instance that this class will use to draw textures
        private Renderer renderer;

        private const int CardTextPadding = 16;
        private const float GlyphLineHeight = 40f;   // height of one line of skin glyph text
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

        // Converts from top-left origin coordinates to the renderer's centre-based coordinates and draws the texture
        private void DrawTopLeftOrigin(Texture texture, CardRect rect)
        {
            renderer.Draw(texture, rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f, rect.Width, rect.Height);
        }

        // Same, for an animated stack item (uses its current Y, not its target)
        private void DrawTopLeftOrigin(Texture texture, StackItem item)
        {
            renderer.Draw(texture, item.X + item.Width / 2f, item.Y + item.Height / 2f, item.Width, item.Height);
        }

        public void DrawChartBrowser(IReadOnlyList<StackItem> items, List<List<ChartBrowserRow>> chartSets,
                                     int chartSetCursor, int chartCursor, StackItem? hoveredItem)
        {
            UploadDecodedBackground();
            DrawTopLeftOrigin(background, backgroundRect);
            //DrawTopLeftOrigin(skin.ChartInfo, chartInfoRect);

            // Empty library (e.g. a fresh build folder with no chart_library.db contents)
            if (chartSets.Count == 0)
            {
                renderer.DrawText(skin.Glyphs, "No charts found", 50, 50, primaryTextColour);
                return;
            }

            var currentSet = chartSets[chartSetCursor];

            // Draw chart cards first as set cards will be drawn on top
            foreach (var item in items)
            {
                if (item.Kind != ChartBrowserState.ChartKind) continue;

                bool isSelected = item.Tag == chartCursor;   // a chart row's tag is its index within the set
                bool isHovered = item == hoveredItem;
                DrawTopLeftOrigin(isSelected || isHovered ? skin.ChartCardHover : skin.ChartCard, item);

                var chart = currentSet[item.Tag];
                DrawCardText(chart.DiffName, item, item.X + CardTextPadding);

                string keyText = chart.KeyCount + "K";
                float keyTextWidth = skin.Glyphs.MeasureText(keyText);
                DrawCardText(keyText, item, item.X + item.Width - CardTextPadding - keyTextWidth);
            }

            // Draw set cards
            foreach (var item in items)
            {
                if (item.Kind != ChartBrowserState.SetKind) continue;

                bool isSelected = item.Tag == 0; // a set row's tag is its slot; slot 0 = the selected set
                bool isHovered = item == hoveredItem;
                DrawTopLeftOrigin(isSelected || isHovered ? skin.SetCardHover : skin.SetCard, item);

                var set = chartSets[chartSetCursor + item.Tag];
                DrawCardText(set[0].Title, item, item.X + CardTextPadding);
            }
        }

        // Draws one line of text at x, vertically centred inside the item
        private void DrawCardText(string text, StackItem item, float x)
        {
            float y = item.Y + (item.Height - GlyphLineHeight) / 2f;
            renderer.DrawText(skin.Glyphs, text, x, y, primaryTextColour);
        }
    }
}