namespace ProjectOdyssey
{
    // Stores a single keyboard input alongside its gameplay timestamp
    public struct InputEvent
    {
        public ushort VKey;
        public bool IsPressed;
        public float TimeStamp;
    }
}