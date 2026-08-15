namespace ProjectOdyssey
{
    public class Note
    {
        public NoteType noteType { get; set; }
        public NoteState noteState { get; set; }
        public byte column { get; set; }
        public double startTime { get; set; }
        public double endTime { get; set; }
        public float headPosY { get; set; }
        public float tailPosY { get; set; }
    }

    public enum NoteType
    {
        Tap,
        Long
    }

    public enum NoteState
    {
        Waiting,
        Holding,
        ReleasedEarly,
        Recovering,
        Resolved
    }
}
