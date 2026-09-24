namespace ProjectOdyssey.Engine
{
    public class Note
    {
        public NoteType NoteType { get; set; }
        public NoteState NoteState { get; set; } = NoteState.Waiting;
        public byte Column { get; set; }
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public float HeadPosY { get; set; }
        public float TailPosY { get; set; }
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
