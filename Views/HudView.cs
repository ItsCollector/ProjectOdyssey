using OpenTK.Mathematics;
using ProjectOdyssey.Engine;
using ProjectOdyssey.Skinning;
using System.Globalization;

namespace ProjectOdyssey.Render
{
    public class HudView
    {
        // Renderer instance that this class will use to draw textures
        private Renderer renderer;

        private readonly HudSkin skin;   // borrowed: owned and disposed by the SkinManager
        private Vector4 primaryTextColour = new Vector4(1f, 1f, 1f, 1f);

        private int cachedCombo = 0;
        private string comboString = "";

        private float cachedAccuracy = 100.00f;
        private string accuracyString = "100.00%";

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

        public HudView(HudSkin skin, Renderer renderer)
        {
            this.skin = skin;
            this.renderer = renderer;
        }

        // currentResult = the result to draw as feedback for the player's worst hit
        // recentJudgementResults = all recent results to use to draw the error graph
        // currentSongTimeMs = the song time "now", used to age out stale results
        public void DrawHud(int incomingCombo, JudgementResult currentResult, ReadOnlySpan<JudgementResult> recentJudgementResults, float currentSongTimeMs, float incomingAccuracy)
        {
            float resultAge = currentSongTimeMs - currentResult.JudgedAtMs;

            if (resultAge >= 0f && resultAge <= JudgementDisplayDurationMs)
            {
                renderer.Draw(skin.Judgements[currentResult.Type], 960, 480);
            }

            if (incomingCombo > 0)
            {
                if (incomingCombo != cachedCombo)
                {
                    cachedCombo = incomingCombo;
                    comboString = incomingCombo.ToString();
                }

                renderer.DrawText(skin.Glyphs, comboString, 960 - (skin.Glyphs.MeasureText(comboString) / 2), 540, primaryTextColour);
            }

            float roundedAccuracy = (float)Math.Round(incomingAccuracy, 2);
            if (roundedAccuracy != cachedAccuracy)
            {
                cachedAccuracy = roundedAccuracy;
                accuracyString = roundedAccuracy.ToString("F2", CultureInfo.InvariantCulture) + "%";
            }

            renderer.DrawText(skin.Glyphs, accuracyString, 50, 50, primaryTextColour);

            foreach (var result in recentJudgementResults)
            {
                float tickAge = currentSongTimeMs - result.JudgedAtMs;
                if (tickAge < 0f || tickAge > ErrorTickDisplayDurationMs) continue;

                float tickX = ErrorGraphCenterX + (result.HitDeviation * ErrorGraphPixelsPerMs);
                Vector4 colour = judgementColours[result.Type];
                colour.W = 1f - (tickAge / ErrorTickDisplayDurationMs); // fade out as it ages

                renderer.DrawQuad(tickX, ErrorGraphY, ErrorTickSize, ErrorTickSize, colour);
            }
        }
    }
}
