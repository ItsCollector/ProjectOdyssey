namespace ProjectOdyssey.Data.Replays
{
    public readonly struct ReplayEvent
    {
        public readonly float SongTimeMs;
        public readonly ushort VKey;
        public readonly bool IsPressed;

        public ReplayEvent(float songTimeMs, ushort vKey, bool isPressed)
        {
            SongTimeMs = songTimeMs;
            VKey = vKey;
            IsPressed = isPressed;
        }
    }
}
