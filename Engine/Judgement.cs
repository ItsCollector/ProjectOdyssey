namespace ProjectOdyssey.Engine
{
    public class Judgement
    {
        public long InputTimestamp;
        public JudgementType Type;
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
