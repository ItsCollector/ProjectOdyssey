using OpenTK.Mathematics;
using ProjectOdyssey.Engine;
using ProjectOdyssey.Screens;
using ProjectOdyssey.Skinning;
using static System.Net.Mime.MediaTypeNames;

namespace ProjectOdyssey.Render
{
    public class HudRenderer : Renderer
    {
        private FontRenderer fontRenderer = new FontRenderer();
        private Dictionary<char, FreeTypeGlyph> glyphs_40;
        private Vector4 primaryTextColour = new Vector4(1f, 1f, 1f, 1f);

        private Dictionary<JudgementType, Texture> judgementTextures = new();

        private int cachedCombo = 0;
        private string comboString = "";

        private float cachedAccuracy = 100f;
        private string accuracyString = "100";

        private const float JudgementDisplayDurationMs = 500f;
        private const float ErrorTickDisplayDurationMs = 500f;

        private const float ErrorGraphCenterX = 960f;
        private const float ErrorGraphY = 700f;
        private const float ErrorGraphPixelsPerMs = 2f;
        private const float ErrorTickSize = 12f;

        private static readonly Dictionary<JudgementType, Vector4> judgementColours = new()
        {
            { JudgementType.Marvellous, new Vector4(0.35f, 0.75f, 1.00f, 1f) },
            { JudgementType.Perfect, new Vector4(1.00f, 0.85f, 0.45f, 1f) },
            { JudgementType.Great, new Vector4(0.35f, 0.85f, 0.40f, 1f) },
            { JudgementType.Good, new Vector4(1.00f, 0.80f, 0.25f, 1f) },
            { JudgementType.Bad, new Vector4(1.00f, 0.45f, 0.25f, 1f) },
            { JudgementType.Miss, new Vector4(0.90f, 0.20f, 0.25f, 1f) },
        };

        public HudRenderer()
        {
            glyphs_40 = fontRenderer.LoadGlyphs(40);

            string baseDir = AppContext.BaseDirectory;
            judgementTextures.Add(JudgementType.Marvellous, LoadTexture(Path.Combine(baseDir, "Assets\\Judgements\\judge-marv.png")));
            judgementTextures.Add(JudgementType.Perfect, LoadTexture(Path.Combine(baseDir, "Assets\\Judgements\\judge-perfect.png")));
            judgementTextures.Add(JudgementType.Great, LoadTexture(Path.Combine(baseDir, "Assets\\Judgements\\judge-great.png")));
            judgementTextures.Add(JudgementType.Good, LoadTexture(Path.Combine(baseDir, "Assets\\Judgements\\judge-good.png")));
            judgementTextures.Add(JudgementType.Bad, LoadTexture(Path.Combine(baseDir, "Assets\\Judgements\\judge-bad.png")));
            judgementTextures.Add(JudgementType.Miss, LoadTexture(Path.Combine(baseDir, "Assets\\Judgements\\judge-miss.png")));
        }

        public void Initialise()
        {
            fontRenderer.Intitialise();
        }

        // currentResult = the result to draw as feedback for the player's worst hit
        // recentJudgementResults = all recent results to use to draw the error graph
        // currentSongTimeMs = the song time "now", used to age out stale results
        public void DrawHud(int incomingCombo, JudgementResult currentResult, JudgementResult[] recentJudgementResults, float currentSongTimeMs, float incomingAccuracy)
        {
            float resultAge = currentSongTimeMs - currentResult.JudgedAtMs;

            if (resultAge >= 0f && resultAge <= JudgementDisplayDurationMs)
            {
                Draw(judgementTextures[currentResult.Type], 960, 480);
            }

            if (incomingCombo > 0)
            {
                if (incomingCombo != cachedCombo)
                {
                    cachedCombo = incomingCombo;
                    comboString = incomingCombo.ToString();
                }

                fontRenderer.Draw(glyphs_40, comboString, 960 - (GlyphOffsetX(comboString, glyphs_40) / 2), 540, primaryTextColour);
            }

            if (incomingAccuracy != cachedAccuracy)
            {
                cachedAccuracy = incomingAccuracy;
                accuracyString = incomingAccuracy.ToString();
            }

            fontRenderer.Draw(glyphs_40, accuracyString + "%", 50, 50, primaryTextColour);

            foreach (var result in recentJudgementResults)
            {
                float tickAge = currentSongTimeMs - result.JudgedAtMs;
                if (tickAge < 0f || tickAge > ErrorTickDisplayDurationMs) continue;

                float tickX = ErrorGraphCenterX + (result.HitDeviation * ErrorGraphPixelsPerMs);
                Vector4 colour = judgementColours[result.Type];
                colour.W = 1f - (tickAge / ErrorTickDisplayDurationMs); // fade out as it ages

                DrawQuad(null, tickX, ErrorGraphY, ErrorTickSize, ErrorTickSize, colour);
            }
        }

        // Helper function to calculate the total width of a string in pixels based on the glyphs
        public int GlyphOffsetX(string text, Dictionary<char, FreeTypeGlyph> glyphs)
        {
            int totalWidth = 0;

            foreach (char c in text)
            {
                if (glyphs.ContainsKey(c))
                {
                    totalWidth += glyphs[c].Advance;
                }
            }

            return totalWidth;
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            fontRenderer.Resize(1920, 1080);
        }

        public override void Dispose()
        {
            fontRenderer.Dispose();

            foreach (var glyph in glyphs_40.Values)
            {
                glyph.Dispose();
            }

            foreach (var texture in judgementTextures.Values)
            {
                texture.Dispose();
            }

            base.Dispose();
        }
    }
}
