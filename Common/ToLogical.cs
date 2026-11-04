using OpenTK.Mathematics;

namespace ProjectOdyssey.Common
{
    public static class ToLogical
    {
        public static Vector2 ViewportToLogical(Vector2 windowPos, Vector2 viewportDimensions)
        {
            return new Vector2(windowPos.X * 1920f / viewportDimensions.X, windowPos.Y * 1080f / viewportDimensions.Y);
        }
    }
}
