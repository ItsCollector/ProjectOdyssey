namespace ProjectOdyssey.Engine
{
    public class Note
    {
        public NoteType noteType { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public NoteState noteState { get; set; } = NoteState.Waiting;
        public byte column { get; set; }
        public float startTime { get; set; }
        public float endTime { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public float headPosY { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
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
