using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace ProjectOdyssey
{
    public class GameplayRenderer : Renderer
    {
        // Gameplay Column Positions
        private int columnStartX;
        private int columnSpacing = 0;
        private int noteWidth = 80;
        private int noteHeight = 50;
        private int headOffset;

        private float[] colX = new float[7];
        private bool notesOverflowPastJudgementLine = false;

        // Judgement Line Position
        public int hitPositionX;
        public int hitPositionY = 1000;
        public int hitPositionWidth;
        public int hitPositionHeight = 50;

        private Texture[] tapNoteTextures = new Texture[7];
        private Texture[] lnHeadTextures = new Texture[7];
        private Texture lnBodyTexture;
        private Texture lnTailTexture;
        private Texture judgementLineTexture;
        private Texture receptorUpTexture;
        private Texture receptorDownTexture;

        public GameplayRenderer(GameplaySkinConfig skinConfig, SkinAssets skinAssets)
        {
            noteWidth = skinConfig.NoteWidth;
            noteHeight = skinConfig.NoteHeight;
            hitPositionX = skinConfig.HitPositionX;
            hitPositionY = skinConfig.HitPositionY;
            columnSpacing = skinConfig.ColumnSpacing;

            hitPositionWidth = noteWidth * 7;
            headOffset = noteHeight / 2;
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

        public void DrawGameplay(Note[][] notesByColumn, int[] columnCursors)
        {
            Draw(judgementLineTexture, hitPositionX, hitPositionY, hitPositionWidth, hitPositionHeight); // judgement line
            //Draw(receptorUpTexture, hitPositionX - headOffset, hitPositionY, noteHeight, noteHeight);

            for (int i = 0; i < notesByColumn.Length; i++)
            {
                for (int j = 0; j < (notesByColumn[i].Length - columnCursors[i]); j++)
                {
                    Note note = notesByColumn[i][j + columnCursors[i]];

                    if (note.headPosY <= 0) // stop drawing if note is not positioned on the screen
                    {
                        break;
                    }

                    if (!notesOverflowPastJudgementLine)
                    {
                        if (note.noteType == NoteType.Tap && note.headPosY >= hitPositionY) continue;
                        if (note.noteType == NoteType.Long && note.tailPosY >= hitPositionY) continue;
                    }

                    float x = colX[i];

                    if (note.noteType == NoteType.Tap)
                    {
                        Draw(tapNoteTextures[i], x, note.headPosY - headOffset, noteWidth, noteHeight);
                    }
                    else
                    {
                        float headCenterY = note.headPosY - headOffset;

                        float bodyHeight = headCenterY - note.tailPosY;
                        float bodyPosY = (headCenterY + note.tailPosY) / 2f;

                        Draw(lnBodyTexture, x, bodyPosY, noteWidth, bodyHeight);
                        Draw(lnHeadTextures[i], x, headCenterY, noteWidth, noteHeight);
                    }
                }
            }
        }

        public void CalculateColumnPositions()
        {
            for (int i = 0; i < 7; i++)
            {
                colX[i] = columnStartX + (noteWidth + columnSpacing) * i + noteWidth / 2f;
                Console.WriteLine($"colX[{i}] = {colX[i]}");
            }
        }
    }
}
