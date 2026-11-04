using OpenTK.Mathematics;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens.Import
{
    public class ImportScreenView
    {
        private readonly Renderer renderer;
        private readonly MenuSkin menuSkin;

        private const float ScreenWidth = 1920f, HeaderMargin = 50f, HeaderTextY = 50f;
        private const float GlyphLineHeight = 40f;   // SkinManager.DefaultGlyphSize

        // Import button (320x90): hit rect is CardRect.FromCenter(ImportButtonCentreX, ImportButtonCentreY, 320, 90)
        public const float ImportButtonCentreX = 960, ImportButtonCentreY = 215;

        // Progress panel (1000x520), top-left. Its right edge lines up with the warning box below it.
        private const float PanelX = 460, PanelY = 290;
        private const float PanelWidth = 1000;

        // Log area inside the panel: below the 60px title bar, 16px side padding
        private const float RowsTop = PanelY + 80;
        private const float RowStep = 44f;
        private const float RowTextLeft = PanelX + 32;
        private const float RowTextMaxWidth = PanelWidth - 64;
        public const int MaxVisibleRows = 9;

        // Warning box under the panel
        private const float WarningY = 835, WarningHeight = 172;
        private const float WarningFirstLineY = WarningY + 20;

        private static readonly string[] warningLines =
        {
            "Do not close the game while importing.",
            "Playing while importing is not recommended,",
            "frame rate may fluctuate."
        };

        private readonly Vector4 primaryTextColour = new Vector4(1f, 1f, 1f, 1f);
        private readonly Vector4 accentTextColour = new Vector4(0.7765f, 0.7216f, 0.9569f, 1f);
        private readonly Vector4 secondaryTextColour = new Vector4(0.6667f, 0.6863f, 0.7451f, 1f);
        private readonly Vector4 warningTextColour = new Vector4(1f, 0.78f, 0.25f, 1f);
        private readonly Vector4 warningBoxColour = new Vector4(1f, 0.78f, 0.25f, 0.10f);

        public ImportScreenView(MenuSkin menuSkin, Renderer renderer)
        {
            this.menuSkin = menuSkin;
            this.renderer = renderer;
        }

        // progressRows: oldest first, newest last, e.g. "Importing Some Song (10/1000)".
        // Only the newest MaxVisibleRows are shown. While importing is true the newest row is highlighted.
        public void DrawImportScreen(IReadOnlyList<string> progressRows, bool importing)
        {
            // Background, header
            DrawAt(menuSkin.Background, 0, 0);
            DrawAt(menuSkin.ResultsHeader, 0, 0);
            renderer.DrawText(menuSkin.Glyphs, "Import", HeaderMargin, HeaderTextY, accentTextColour);

            // Button and progress panel
            renderer.Draw(menuSkin.ImportButton, ImportButtonCentreX, ImportButtonCentreY);
            DrawAt(menuSkin.ImportProgressPanel, PanelX, PanelY);

            DrawRows(progressRows, importing);
            DrawWarning();
        }

        private void DrawRows(IReadOnlyList<string> rows, bool importing)
        {
            if (rows.Count == 0)
            {
                renderer.DrawText(menuSkin.Glyphs, "Press Import to begin.", RowTextLeft, RowsTop, secondaryTextColour);
                return;
            }

            int first = Math.Max(0, rows.Count - MaxVisibleRows);
            for (int i = first; i < rows.Count; i++)
            {
                bool isNewest = i == rows.Count - 1;
                Vector4 colour = (isNewest && importing) ? accentTextColour : secondaryTextColour;
                float y = RowsTop + (i - first) * RowStep;

                renderer.DrawText(menuSkin.Glyphs, FitToWidth(rows[i], RowTextMaxWidth), RowTextLeft, y, colour);
            }
        }

        private void DrawWarning()
        {
            renderer.DrawQuad(PanelX + PanelWidth / 2f, WarningY + WarningHeight / 2f, PanelWidth, WarningHeight, warningBoxColour);

            for (int i = 0; i < warningLines.Length; i++)
            {
                string line = warningLines[i];
                float x = PanelX + (PanelWidth - menuSkin.Glyphs.MeasureText(line)) / 2f;   // centred in the box
                Vector4 colour = i == 0 ? warningTextColour : primaryTextColour;
                renderer.DrawText(menuSkin.Glyphs, line, x, WarningFirstLineY + i * RowStep, colour);
            }
        }

        // Long names get cut with "..." so a row never spills out of the box. Only allocates when a row is too long.
        private string FitToWidth(string text, float maxWidth)
        {
            if (menuSkin.Glyphs.MeasureText(text) <= maxWidth) return text;

            int length = text.Length;
            while (length > 1 && menuSkin.Glyphs.MeasureText(string.Concat(text.AsSpan(0, length), "...")) > maxWidth)
            {
                length--;
            }
            return string.Concat(text.AsSpan(0, length), "...");
        }

        // Draws a texture with its top-left corner at (x, y). renderer.Draw wants the centre, so shift by half the size.
        private void DrawAt(Texture texture, float x, float y)
        {
            renderer.Draw(texture, x + texture.Width / 2f, y + texture.Height / 2f);
        }
    }
}