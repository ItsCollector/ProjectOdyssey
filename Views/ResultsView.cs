using OpenTK.Mathematics;
using ProjectOdyssey.Engine;
using ProjectOdyssey.Engine.Scoring;
using ProjectOdyssey.Render;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Views
{
    public class ResultsView
    {
        // Renderer instance that this class will use to draw textures
        private Renderer renderer;
        private readonly MenuSkin menuSkin;

        private Vector4 primaryTextColour = new Vector4(1f, 1f, 1f, 1f);

        private static readonly Dictionary<JudgementType, Vector4> judgementColours = new()
        {
            { JudgementType.Marvellous, new Vector4(0.35f, 0.75f, 1.00f, 1f) },
            { JudgementType.Perfect, new Vector4(1.00f, 0.85f, 0.45f, 1f) },
            { JudgementType.Great, new Vector4(0.35f, 0.85f, 0.40f, 1f) },
            { JudgementType.Good, new Vector4(1.00f, 0.80f, 0.25f, 1f) },
            { JudgementType.Bad, new Vector4(1.00f, 0.45f, 0.25f, 1f) },
            { JudgementType.Miss, new Vector4(0.90f, 0.20f, 0.25f, 1f) },
        };

        public ResultsView(MenuSkin menuSkin, Renderer renderer)
        {
            this.menuSkin = menuSkin;
            this.renderer = renderer;
        }

        public void DrawResultsScreen(ScoreData scoreData)
        {
            renderer.DrawText(menuSkin.Glyphs, "Results", 30, 30, primaryTextColour);
            renderer.DrawText(menuSkin.Glyphs, $"Marvellous: {scoreData.JudgementCounts[JudgementType.Marvellous]}", 30, 90, judgementColours[JudgementType.Marvellous]);
            renderer.DrawText(menuSkin.Glyphs, $"Perfect: {scoreData.JudgementCounts[JudgementType.Perfect]}", 30, 150, judgementColours[JudgementType.Perfect]);
            renderer.DrawText(menuSkin.Glyphs, $"Great: {scoreData.JudgementCounts[JudgementType.Great]}", 30, 210, judgementColours[JudgementType.Great]);
            renderer.DrawText(menuSkin.Glyphs, $"Good: {scoreData.JudgementCounts[JudgementType.Good]}", 30, 270, judgementColours[JudgementType.Good]);
            renderer.DrawText(menuSkin.Glyphs, $"Bad: {scoreData.JudgementCounts[JudgementType.Bad]}", 30, 330, judgementColours[JudgementType.Bad]);
            renderer.DrawText(menuSkin.Glyphs, $"Miss: {scoreData.JudgementCounts[JudgementType.Miss]}", 30, 390, judgementColours[JudgementType.Miss]);
            renderer.DrawText(menuSkin.Glyphs, $"Max Combo: {scoreData.HighestCombo}", 30, 450, primaryTextColour);
            renderer.DrawText(menuSkin.Glyphs, $"Pause Count: {scoreData.PauseCount}", 30, 510, primaryTextColour);

            renderer.DrawText(menuSkin.Glyphs, $"Continuous Accuracy: {scoreData.CurrentContinuousAccuracy}%", 30, 570, primaryTextColour);
            renderer.DrawText(menuSkin.Glyphs, $"Discrete Accuracy: {scoreData.CurrentDiscreteAccuracy}%", 30, 630, primaryTextColour);
        }
    }
}
