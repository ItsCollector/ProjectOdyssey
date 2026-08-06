namespace ProjectOdyssey
{
    public class Judgement
    {
        public long inputTimestamp;
        public JudgementType type;
    }

    public class TapNoteJudgementState
    {
        public Judgement? judgement;
        public bool isResolved;
    }

    public class LongNoteJudgementState
    {
        public Judgement? head;
        public Judgement? tail;
        public NoteState state;
    }

    public enum NoteState
    {
        WaitingForHead,
        Holding,
        ReleasedEarly,
        Resolved
    }

    public enum JudgementType
    {
        Marvellous,
        Perfect,
        Great,
        Good,
        Bad,
        Miss
    }

    public enum InputDirection
    {
        Down,
        Up
    }
}
