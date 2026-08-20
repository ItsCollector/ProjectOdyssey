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
        public LongNoteHoldState holdState;
        public bool isResolved => holdState == LongNoteHoldState.Resolved;
    }

    public enum LongNoteHoldState
    {
        WaitingForHead,
        Holding,
        ReleasedEarly,
        Recovering,
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
