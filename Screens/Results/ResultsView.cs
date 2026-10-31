using OpenTK.Mathematics;
using ProjectOdyssey.Engine.Scoring;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.Screens.Components;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens.Results
{
    public class ResultsView
    {
        // Renderer instance that this class will use to draw textures
        private Renderer renderer;
        private readonly MenuSkin menuSkin;

        private const float JudgementPanelX = 60, JudgementPanelY = 190;
        private const float GradeFrameX = 780, GradeFrameY = 190;
        private const float StatsPanelX = 1220, StatsPanelY = 190;
        private const float StatsPanelWidth = 640, StatsPanelHeight = 430;   // results_stats_panel.png

        // Buttons sit in a column under the stats panel, right-aligned to its right edge.
        // Order top to bottom: retry, watch replay, back. X is worked out from each texture's width at draw time.
        private const float ButtonGap = 30f;
        private const float ButtonColumnRight = StatsPanelX + StatsPanelWidth;
        private const float ButtonColumnTop = StatsPanelY + StatsPanelHeight + ButtonGap;

        private const float GlyphLineHeight = 40f;   // SkinManager.DefaultGlyphSize

        // Judgement rows (panel coordinates): six 70px rows, first one at y=70, 78px apart
        private const float JudgementRowsStart = 70, JudgementRowStep = 78, JudgementRowHeight = 70;
        private const float JudgementTextLeft = 40, JudgementTextRight = 624;

        // Stats slots (panel coordinates): X, Y, Width, Height
        private readonly record struct Slot(float X, float Y, float W, float H);
        private static readonly Slot ContinuousAccuracySlot = new(16, 70, 608, 110);
        private static readonly Slot DiscreteAccuracySlot = new(16, 190, 608, 110);
        private static readonly Slot ComboSlot = new(16, 310, 300, 110);
        private static readonly Slot PausesSlot = new(324, 310, 300, 110);

        // Header text: the left and right margins and the line's top Y (a 40px line centred in the 140px header)
        private const float ScreenWidth = 1920f, HeaderMargin = 50f, HeaderTextY = 50f;

        private const float SlotPadding = 12;   // gap between a slot's edge and its label
        private const float SlotValueOffset = 56;   // value line sits this far below the label line

        // Set to true to draw a translucent box over every text slot, to check text placement against the panels.
        private const bool ShowSlotOutlines = false;

        private Vector4 primaryTextColour = new Vector4(1f, 1f, 1f, 1f);
        private Vector4 accentTextColour = new Vector4(0.7765f, 0.7216f, 0.9569f, 1f);
        private Vector4 secondaryTextColour = new Vector4(0.6667f, 0.6863f, 0.7451f, 1f);

        private static readonly Dictionary<JudgementType, Vector4> judgementColours = new()
        {
            { JudgementType.Marvellous, new Vector4(0.35f, 0.75f, 1.00f, 1f) },
            { JudgementType.Perfect, new Vector4(1.00f, 0.85f, 0.45f, 1f) },
            { JudgementType.Great, new Vector4(0.35f, 0.85f, 0.40f, 1f) },
            { JudgementType.Good, new Vector4(1.00f, 0.80f, 0.25f, 1f) },
            { JudgementType.Bad, new Vector4(1.00f, 0.45f, 0.25f, 1f) },
            { JudgementType.Miss, new Vector4(0.90f, 0.20f, 0.25f, 1f) },
        };

        // Display order, top to bottom; the row index is the position in this array
        private static readonly JudgementType[] judgementRows =
        {
            JudgementType.Marvellous, JudgementType.Perfect, JudgementType.Great,
            JudgementType.Good, JudgementType.Bad, JudgementType.Miss
        };

        public ResultsView(MenuSkin menuSkin, Renderer renderer)
        {
            this.menuSkin = menuSkin;
            this.renderer = renderer;
        }

        public void DrawResultsScreen(ScoreData scoreData)
        {
            // Backgrounds and panels
            DrawAt(menuSkin.ResultsBackground, 0, 0);
            DrawAt(menuSkin.ResultsHeader, 0, 0);
            DrawAt(menuSkin.ResultsJudgementPanel, JudgementPanelX, JudgementPanelY);
            //DrawAt(menuSkin.ResultsGradeFrame, GradeFrameX, GradeFrameY); 
            DrawAt(menuSkin.ResultsStatsPanel, StatsPanelX, StatsPanelY);

            // Buttons: retry, watch replay, back
            float buttonY = ButtonColumnTop;
            buttonY = DrawColumnButton(menuSkin.ResultsRetryButton, buttonY);
            buttonY = DrawColumnButton(menuSkin.ResultsReplayButton, buttonY);
            DrawColumnButton(menuSkin.ResultsBackButton, buttonY);

            // Header: title on the left, date and time the score was set on the right
            renderer.DrawText(menuSkin.Glyphs, "Results", HeaderMargin, HeaderTextY, accentTextColour);

            string dateText = FormatDate(scoreData.SetTimeUtc);
            float dateWidth = menuSkin.Glyphs.MeasureText(dateText);
            renderer.DrawText(menuSkin.Glyphs, dateText, ScreenWidth - HeaderMargin - dateWidth, HeaderTextY, secondaryTextColour);

            // Judgement rows: name on the left, count right-aligned
            for (int row = 0; row < judgementRows.Length; row++)
            {
                JudgementType type = judgementRows[row];
                DrawJudgementRow(row, type.ToString(), scoreData.JudgementCounts[type], judgementColours[type]);
            }

            // Stats panel
            DrawStat(ContinuousAccuracySlot, "Continuous Accuracy", $"{scoreData.CurrentContinuousAccuracy:F2}%");
            DrawStat(DiscreteAccuracySlot, "Discrete Accuracy", $"{scoreData.CurrentDiscreteAccuracy:F2}%");
            DrawStat(ComboSlot, "Max Combo", scoreData.HighestCombo.ToString());
            DrawStat(PausesSlot, "Pauses", scoreData.PauseCount.ToString());
        }

        // Draws a texture with its top-left corner at (x, y). renderer.Draw wants the centre, so shift by half the size.
        private void DrawAt(Texture texture, float x, float y)
        {
            renderer.Draw(texture, x + texture.Width / 2f, y + texture.Height / 2f);
        }

        // Draws a button right-aligned to the column edge with its top at y; returns the Y for the next button down.
        private float DrawColumnButton(Texture texture, float y)
        {
            DrawAt(texture, ButtonColumnRight - texture.Width, y);
            return y + texture.Height + ButtonGap;
        }

        private void DrawJudgementRow(int row, string name, int count, Vector4 colour)
        {
            float rowTop = JudgementPanelY + JudgementRowsStart + row * JudgementRowStep;
            float textY = rowTop + (JudgementRowHeight - GlyphLineHeight) / 2f;   // centre one line in the row

            string countText = count.ToString();
            float countWidth = menuSkin.Glyphs.MeasureText(countText);

            renderer.DrawText(menuSkin.Glyphs, name, JudgementPanelX + JudgementTextLeft, textY, colour);
            renderer.DrawText(menuSkin.Glyphs, countText, JudgementPanelX + JudgementTextRight - countWidth, textY, colour);
        }

        // One slot in the stats panel: a small label on top, the value underneath.
        private void DrawStat(Slot slot, string label, string value)
        {
            float x = StatsPanelX + slot.X + SlotPadding;
            float y = StatsPanelY + slot.Y + SlotPadding;

            renderer.DrawText(menuSkin.Glyphs, label, x, y, secondaryTextColour);
            renderer.DrawText(menuSkin.Glyphs, value, x, y + SlotValueOffset, primaryTextColour);

            if (ShowSlotOutlines)
            {
                // DrawQuad is centre-based too
                renderer.DrawQuad(StatsPanelX + slot.X + slot.W / 2f, StatsPanelY + slot.Y + slot.H / 2f,
                                  slot.W, slot.H, new Vector4(1f, 0.9f, 0f, 0.25f));
            }
        }

        // SetTimeUtc is .NET ticks (DateTime.UtcNow.Ticks)
        private static string FormatDate(long utcTicks)
        {
            if (utcTicks <= 0) return "-";
            return new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime().ToString("dd MMM yyyy HH:mm");
        }
    }
}