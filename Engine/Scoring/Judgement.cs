namespace ProjectOdyssey.Engine
{
    public readonly struct JudgementResult
    {
        public JudgementType Type { get; }
        public float HitDeviation { get; }
        public float JudgedAtMs { get; }

        public JudgementResult(JudgementType type, float hitDeviation, float judgedAtMs)
        {
            Type = type;
            HitDeviation = hitDeviation;
            JudgedAtMs = judgedAtMs;
        }
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
