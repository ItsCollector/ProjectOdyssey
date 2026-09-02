namespace ProjectOdyssey
{
    public class Judgement
    {
        public long inputTimestamp;
        public JudgementType type;
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
