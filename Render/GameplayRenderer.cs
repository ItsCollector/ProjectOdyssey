using ProjectOdyssey.Engine;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Render
{
    public class GameplayRenderer : Renderer
    {
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

        public GameplayRenderer(GameplaySkinConfig skinConfig, SkinAssets skinAssets)
        {
            noteWidth = skinConfig.NoteWidth;
            hitPositionX = skinConfig.HitPositionX;
            hitPositionY = skinConfig.HitPositionY;
            columnSpacing = skinConfig.ColumnSpacing;
            targetType = skinConfig.TargetType;

            hitPositionWidth = noteWidth * 7;
            headOffset = noteWidth / 2;
            columnStartX = hitPositionX - (hitPositionWidth / 2);

            CalculateColumnPositions();

            Texture[] tapVariants = skinAssets.TapNotePaths.Select(LoadTexture).ToArray();
            Texture[] lnHeadVariants = skinAssets.LnHeadPaths.Select(LoadTexture).ToArray();

            for (int i = 0; i < 7; i++)
            {
                tapNoteTextures[i] = tapVariants[i % tapVariants.Length];
                lnHeadTextures[i] = lnHeadVariants[i % lnHeadVariants.Length];
            }

            lnBodyTexture = LoadTexture(skinAssets.LnBodyPath);
            lnTailTexture = LoadTexture(skinAssets.LnTailPath);
            judgementLineTexture = LoadTexture(skinAssets.JudgementLinePath);
            receptorUpTexture = LoadTexture(skinAssets.ReceptorUpPath);
            receptorDownTexture = LoadTexture(skinAssets.ReceptorDownPath);
        }

        /*  Something to return to later, the note height value is unused because I use a circle skin and the images are square.
         *  I haven't decided fully whether skins will specify height and use them, or add an offset value to 
         *  indicate that the bottom of the image is not the bottom of the note visually. */

        public void DrawGameplay(Note[][] notesByColumn, int[] columnCursors)
        {
            if (targetType == TargetType.Line)
            {
                Draw(judgementLineTexture, hitPositionX, hitPositionY, hitPositionWidth, hitPositionHeight);
            }
            else
            {
                for (int i = 0; i < notesByColumn.Length; i++)
                {
                    Draw(receptorDownTexture, columnStartX + (noteWidth * i) + (noteWidth / 2), hitPositionY - headOffset, noteWidth, noteWidth);
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
                        Draw(tapNoteTextures[i], x, note.HeadPosY - headOffset, noteWidth, noteWidth);
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

                        Draw(lnBodyTexture, x, bodyPosY, noteWidth, Math.Max(bodyHeight, 0f));
                        DrawClippedBelow(lnTailTexture, x, tailCenterY, noteWidth, noteWidth, headCenterY);
                        Draw(lnHeadTextures[i], x, headCenterY, noteWidth, noteWidth);
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