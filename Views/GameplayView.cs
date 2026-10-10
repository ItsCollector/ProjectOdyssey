using ProjectOdyssey.Engine;
using ProjectOdyssey.Skinning;
using ProjectOdyssey.Render;

namespace ProjectOdyssey.Render
{
    public class GameplayView
    {
        // Renderer instance that this class will use to draw textures
        private readonly Renderer renderer;

        // Gameplay Column Positions
        private readonly int keyCount;
        private readonly int columnStartX;
        private readonly int columnSpacing;
        private readonly int noteWidth;
        private readonly int headOffset;

        private readonly float[] colX;
        private bool notesOverflowPastJudgementLine = false;

        // Judgement Line Position
        private readonly int hitPositionX;
        private readonly int hitPositionY;
        private readonly int hitPositionWidth;
        private int hitPositionHeight = 50;

        // Textures, one per column (the SkinManager has already expanded them to the key count)
        private readonly Texture[] tapNoteTextures;
        private readonly Texture[] lnHeadTextures;
        private readonly Texture[] lnBodyTextures;
        private readonly Texture[] lnTailTextures;

        // Only the ones the target type uses are loaded
        private readonly Texture? judgementLineTexture;
        private readonly Texture? receptorUpTexture;
        private readonly Texture? receptorDownTexture;

        // Other
        private readonly TargetType targetType;

        // All textures are borrowed from the SkinManager (it owns and disposes them),
        // so this renderer has nothing of its own to dispose beyond the base class.
        public GameplayView(GameplaySkin skin, Renderer renderer)
        {
            this.renderer = renderer;

            keyCount = skin.KeyCount;
            noteWidth = skin.NoteWidth;
            hitPositionX = skin.HitPositionX;
            hitPositionY = skin.HitPositionY;
            columnSpacing = skin.ColumnSpacing;
            targetType = skin.TargetType;

            hitPositionWidth = noteWidth * keyCount + columnSpacing * (keyCount - 1);
            headOffset = noteWidth / 2;
            columnStartX = hitPositionX - (hitPositionWidth / 2);

            colX = new float[keyCount];
            CalculateColumnPositions();

            tapNoteTextures = skin.TapNotes;
            lnHeadTextures = skin.LnHeads;
            lnBodyTextures = skin.LnBodies;
            lnTailTextures = skin.LnTails;

            judgementLineTexture = skin.JudgementLine;
            receptorUpTexture = skin.ReceptorUp;
            receptorDownTexture = skin.ReceptorDown;
        }

        /*  Something to return to later, the note height value is unused because I use a circle skin and the images are square.
         *  I haven't decided fully whether skins will specify height and use them, or add an offset value to 
         *  indicate that the bottom of the image is not the bottom of the note visually. */

        public void DrawGameplay(Note[][] notesByColumn, int[] columnCursors)
        {
            if (targetType == TargetType.Line)
            {
                if (judgementLineTexture != null)
                    renderer.Draw(judgementLineTexture, hitPositionX, hitPositionY, hitPositionWidth, hitPositionHeight);
            }
            else if (receptorDownTexture != null)
            {
                for (int i = 0; i < notesByColumn.Length; i++)
                {
                    renderer.Draw(receptorDownTexture, colX[i], hitPositionY - headOffset, noteWidth, noteWidth);
                }
            }

            for (int i = 0; i < notesByColumn.Length; i++)
            {
                for (int j = 0; j < (notesByColumn[i].Length - columnCursors[i]); j++)
                {
                    Note note = notesByColumn[i][j + columnCursors[i]];

                    if (note.HeadPosY <= 0) break;

                    if (!notesOverflowPastJudgementLine)
                    {
                        if (note.NoteType == NoteType.Tap && note.HeadPosY >= hitPositionY) continue;
                        if (note.NoteType == NoteType.Long && (note.TailPosY + noteWidth) >= hitPositionY) continue;
                    }

                    float x = colX[i];

                    if (note.NoteType == NoteType.Tap)
                    {
                        renderer.Draw(tapNoteTextures[i], x, note.HeadPosY - headOffset, noteWidth, noteWidth);
                    }
                    else
                    {
                        bool anchorHead = notesOverflowPastJudgementLine && note.NoteState == NoteState.Holding;
                        float effectiveHeadPosY = anchorHead ? Math.Min(note.HeadPosY, hitPositionY) : note.HeadPosY;

                        float headCenterY = effectiveHeadPosY - headOffset;
                        float tailCenterY = note.TailPosY + headOffset;
                        float tailBottomEdge = note.TailPosY + noteWidth;
                        float bodyHeight = headCenterY - tailBottomEdge;
                        float bodyPosY = (headCenterY + tailBottomEdge) / 2f;

                        renderer.Draw(lnBodyTextures[i], x, bodyPosY, noteWidth, Math.Max(bodyHeight, 0f));
                        renderer.DrawClippedBelow(lnTailTextures[i], x, tailCenterY, noteWidth, noteWidth, headCenterY);
                        renderer.Draw(lnHeadTextures[i], x, headCenterY, noteWidth, noteWidth);
                    }
                }
            }
        }

        private void CalculateColumnPositions()
        {
            for (int i = 0; i < keyCount; i++)
            {
                colX[i] = columnStartX + (noteWidth + columnSpacing) * i + noteWidth / 2f;
            }
        }
    }
}