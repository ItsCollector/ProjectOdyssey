namespace ProjectOdyssey
{
    public class GameplayRenderer : Renderer
    {
        // Active Gameplay Textures
        public Dictionary<string, Texture> textures = new Dictionary<string, Texture>();

        // Gameplay Column Positions
        int startX = 720;
        int noteWidth = 120;
        float[] colX = new float[5];

        // Judgement Line Position
        public int hitY = 900;

        public void LoadTextures()
        {
            // take a list of textures and load each one

            CalculateColumnPositions();
            Resize(1920, 1080);
        }

        public void CalculateColumnPositions()
        {
            for (int i = 1; i <= 4; i++)
            {
                colX[i] = startX + noteWidth * (i - 1) + noteWidth / 2f;
            }
        }
    }
}
