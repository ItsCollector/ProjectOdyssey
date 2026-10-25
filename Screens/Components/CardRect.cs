namespace ProjectOdyssey.Screens
{
    public struct CardRect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        // Checks if a point (px, py) is inside the rectangle defined by this CardRect.
        public bool Contains(float px, float py)
        {
            bool insideHorizontally = px >= X && px < X + Width;
            bool insideVertically = py >= Y && py < Y + Height;

            return insideHorizontally && insideVertically;
        }

        public static CardRect FromCenter(float centerX, float centerY, float width, float height) => new()
        {
            X = centerX - width / 2f,
            Y = centerY - height / 2f,
            Width = width,
            Height = height
        };
    }
}
