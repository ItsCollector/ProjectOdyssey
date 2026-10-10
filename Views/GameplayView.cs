using ProjectOdyssey.Engine;
using ProjectOdyssey.Skinning;
using ProjectOdyssey.Render;

namespace ProjectOdyssey.Render
{
    public class GameplayView
    {
        // Renderer instance that this class will use to draw textures
        private Renderer renderer;

        // Gameplay Column Positions
        private int columnStartX;
        private int columnSpacing = 0;
        private int noteWidth = 80;
        private int headOffset;

        private float[] colX = new float[7];
        private bool notesOverflowPastJudgementLine = false;

        // Judgement Line Position
        private int hitPositionX;
        private int hitPositionY = 1000;
        private int hitPositionWidth;
        private int hitPositionHeight = 50;

        // Textures
        private Texture[] tapNoteTextures = new Texture[7];
        private Texture[] lnHeadTextures = new Texture[7];
        private Texture lnBodyTexture;
        private Texture lnTailTexture;
        private Texture judgementLineTexture;
        private Texture receptorUpTexture;
        private Texture receptorDownTexture;

        // Other
        private TargetType targetType;

        // All textures are borrowed from the SkinManager (it owns and disposes them),
        // so this renderer has nothing of its own to dispose beyond the base class.
        public GameplayView(GameplaySkin skin, Renderer renderer, byte keyCount)
        {
            GameplaySkinConfig skinConfig = skin.Config;
            this.renderer = renderer;

            noteWidth = skinConfig.NoteWidth;
            hitPositionX = skinConfig.HitPositionX;
            hitPositionY = skinConfig.HitPositionY;
            columnSpacing = skinConfig.ColumnSpacing;
            targetType = skinConfig.TargetType;

            hitPositionWidth = noteWidth * keyCount;
            headOffset = noteWidth / 2;
            columnStartX = hitPositionX - (hitPositionWidth / 2);

            CalculateColumnPositions();

            // Fewer variants than columns? Cycle through them.
            for (int i = 0; i < keyCount; i++)
            {
                tapNoteTextures[i] = skin.TapNotes[i % skin.TapNotes.Length];
                lnHeadTextures[i] = skin.LnHeads[i % skin.LnHeads.Length];
            }

            lnBodyTexture = skin.LnBody;
            lnTailTexture = skin.LnTail;
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
                renderer.Draw(judgementLineTexture, hitPositionX, hitPositionY, hitPositionWidth, hitPositionHeight);
            }
            else
            {
                for (int i = 0; i < notesByColumn.Length; i++)
                {
                    renderer.Draw(receptorDownTexture, columnStartX + (noteWidth * i) + (noteWidth / 2), hitPositionY - headOffset, noteWidth, noteWidth);
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

                        renderer.Draw(lnBodyTexture, x, bodyPosY, noteWidth, Math.Max(bodyHeight, 0f));
                        renderer.DrawClippedBelow(lnTailTexture, x, tailCenterY, noteWidth, noteWidth, headCenterY);
                        renderer.Draw(lnHeadTextures[i], x, headCenterY, noteWidth, noteWidth);
                    }
                }
            }
        }

        public void CalculateColumnPositions()
        {
            for (int i = 0; i < 7; i++)
            {
                colX[i] = columnStartX + (noteWidth + columnSpacing) * i + noteWidth / 2f;
            }
        }
    }
}
