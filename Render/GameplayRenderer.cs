namespace ProjectOdyssey
{
    public class GameplayRenderer : Renderer
    {
        // Gameplay Column Positions
        private int columnStartX;
        private int columnSpacing = 0;
        private int noteWidth = 80;     
        private int headOffset;         

        private float[] colX = new float[7];
        private bool notesOverflowPastJudgementLine = true;

        // Judgement Line Position
        public int hitPositionX;
        public int hitPositionY = 1000;
        public int hitPositionWidth;
        public int hitPositionHeight = 50;

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

                    if (note.headPosY <= 0) break;

                    if (!notesOverflowPastJudgementLine)
                    {
                        if (note.noteType == NoteType.Tap && note.headPosY >= hitPositionY) continue;
                        if (note.noteType == NoteType.Long && (note.tailPosY + noteWidth) >= hitPositionY) continue;
                    }

                    float x = colX[i];

                    if (note.noteType == NoteType.Tap)
                    {
                        Draw(tapNoteTextures[i], x, note.headPosY - headOffset, noteWidth, noteWidth);
                    }
                    else
                    {
                        bool anchorHead = notesOverflowPastJudgementLine && note.noteState == NoteState.Holding;
                        float effectiveHeadPosY = anchorHead ? Math.Min(note.headPosY, hitPositionY) : note.headPosY;

                        float headCenterY = effectiveHeadPosY - headOffset;
                        float tailCenterY = note.tailPosY + headOffset;
                        float tailBottomEdge = note.tailPosY + noteWidth;

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