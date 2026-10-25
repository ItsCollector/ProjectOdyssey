namespace ProjectOdyssey.Engine.Scoring
{
    public struct ScoreData
    {
        public Dictionary<JudgementType, int> JudgementCounts { get; set; }
        public int JudgedNotesCount { get; set; }
        public float CurrentDiscreteAccuracy { get; set; }
        public float CurrentContinuousAccuracy { get; set; }
        public int HighestCombo { get; set; }
        public int PauseCount { get; set; }
        public long SetTimeUtc { get; set; }

        public ScoreData(Dictionary<JudgementType, int> judgementCounts, int judgedNotesCount, float currentDiscreteAccuracy, float currentContinuousAccuracy, int highestCombo, int pauseCount)
        {
            JudgementCounts = judgementCounts;
            JudgedNotesCount = judgedNotesCount;
            CurrentDiscreteAccuracy = currentDiscreteAccuracy;
            CurrentContinuousAccuracy = currentContinuousAccuracy;
            HighestCombo = highestCombo;
            PauseCount = pauseCount;
            SetTimeUtc = DateTime.UtcNow.Ticks;
        }
    }
}
