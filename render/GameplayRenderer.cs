namespace ProjectOdyssey
{
    public class GameplayRenderer : Renderer
    {
        // Active Gameplay Textures
        public Dictionary<string, Texture> textures = new Dictionary<string, Texture>();

        // Gameplay Column Positions
        private int startX = 720;
        private const int noteWidth = 80;
        private const int noteHeight = 50;
        private const int headOffset = noteHeight / 2;

        private float[] colX = new float[7];
        private bool notesOverflowPastJudgementLine = true;

        // Judgement Line Position
        public int hitY = 1000;

        public void LoadTextures()
        {
            // take a list of textures and load each one

            CalculateColumnPositions();
            Resize(1920, 1080);
        }

        public void DrawGameplay(Note[][] notesByColumn, int[] columnCursors)
        {
            for (int i = 0; i < notesByColumn.Length; i++)
            {
                for (int j = 0; j < (notesByColumn[i].Length - columnCursors[i]); j++)
                {
                    Note note = notesByColumn[i][j + columnCursors[i]];

                    if (note.headPosY <= 0) // stop drawing if note is not positioned on the screen
                    {
                        break;
                    }

                    if (!notesOverflowPastJudgementLine && note.tailPosY >= hitY)
                    {
                        continue;
                    }

                    float x = colX[i];

                    if (note.noteType == NoteType.Tap)
                    {
                        Draw(null, x, note.headPosY - headOffset, noteWidth, noteHeight);
                    }
                    else
                    {
                        float bodyHeight = note.headPosY - note.tailPosY;
                        float bodyPosY = (note.headPosY + note.tailPosY) / 2f;
                        Draw(null, x, bodyPosY, noteWidth, bodyHeight);
                    }
                }
            }
        }

        public void CalculateColumnPositions()
        {
            for (int i = 0; i <= 6; i++)
            {
                colX[i] = startX + noteWidth * i + noteWidth / 2f;
                Console.WriteLine($"colX[{i}] = {colX[i]}");
            }

            
        }
    }
}
