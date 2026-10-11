using ProjectOdyssey.Engine.Mechanics;
using ProjectOdyssey.Engine.Rulesets;

namespace ProjectOdyssey.Engine.Scoring
{
    // Conducts accuracy calculation and makes it available for different systems to access
    public class ScoreManager
    {
        // Gameplay Mechanics
        private RulesetMechanics mechanics;

        // Judgements
        public Dictionary<JudgementType, int> JudgementCounts { get; private set; } = new();
        public int JudgedNotesCount = 0;

        public float CurrentDiscreteAccuracy = 100.0f;
        public float CurrentContinuousAccuracy = 100.0f;

        public float ContinuousAccuracyAccumulator = 0.0f;
        public float DiscreteAccuracyAccumulator = 0.0f;

        // Combo 
        public int CurrentCombo = 0;
        public int HighestCombo = 0;

        public ScoreManager(RulesetMechanics mechanics)
        {
            foreach (JudgementType type in Enum.GetValues<JudgementType>())
            {
                JudgementCounts[type] = 0;
            }

            this.mechanics = mechanics;
        }

        public void AccumulateJudgement(JudgementType judgement)
        {
            if (JudgementCounts.ContainsKey(judgement))
            {
                JudgementCounts[judgement]++;
            }
            else
            {
                JudgementCounts[judgement] = 1;
            }

            JudgedNotesCount++;
        }

        public void CalculateAccuracyContinuous(JudgementType judgement, float hitDeviation)
        {
            float absHitDeviation = Math.Max(Math.Abs(hitDeviation), mechanics.WindowStart(judgement));
            absHitDeviation = Math.Clamp(absHitDeviation, 0f, mechanics.GhostTapThreshold);

            float decay = 0.3f;
            float normalised = 1.0f - (absHitDeviation / mechanics.GhostTapThreshold);
            float accuracyContribution = (float)Math.Pow(normalised, decay);

            ContinuousAccuracyAccumulator += accuracyContribution;

            if (JudgedNotesCount == 0)
            {
                CurrentContinuousAccuracy = 100f;
            }
            else
            {
                CurrentContinuousAccuracy = (ContinuousAccuracyAccumulator / JudgedNotesCount) * 100f;
            }
        }

        public void CalculateAccuracyDiscrete(JudgementType judgement)
        {
            float accuracyContribution = judgement switch
            {
                JudgementType.Marvellous => 1.0f,
                JudgementType.Perfect => 0.9f,
                JudgementType.Great => 0.5f,
                JudgementType.Good => -0.5f,
                JudgementType.Bad => -1f,
                JudgementType.Miss => -1f,
                _ => 0.0f
            };

            DiscreteAccuracyAccumulator += accuracyContribution;

            if (JudgedNotesCount == 0)
            {
                CurrentDiscreteAccuracy = 100f;
            }
            else
            {
                CurrentDiscreteAccuracy = (DiscreteAccuracyAccumulator / JudgedNotesCount) * 100f;
            }
        }

        public void UpdateCombo(ComboEffect comboEffect)
        {
            switch (comboEffect)
            {
                case ComboEffect.Increment:
                    CurrentCombo++;
                    if (CurrentCombo > HighestCombo) HighestCombo = CurrentCombo;
                    break;
                case ComboEffect.Break:
                    CurrentCombo = 0;
                    break;
            }
        }

        public ScoreData CompileScoreData()
        {
            return new ScoreData(
                new Dictionary<JudgementType, int>(JudgementCounts),
                JudgedNotesCount,
                CurrentDiscreteAccuracy,
                CurrentContinuousAccuracy,
                HighestCombo
            );
        }
    }

    public struct ScoreData
    {
        public Dictionary<JudgementType, int> JudgementCounts { get; set; }
        public int JudgedNotesCount { get; set; }
        public float CurrentDiscreteAccuracy { get; set; }
        public float CurrentContinuousAccuracy { get; set; }
        public int HighestCombo { get; set; }

        public ScoreData(Dictionary<JudgementType, int> judgementCounts, int judgedNotesCount, float currentDiscreteAccuracy, float currentContinuousAccuracy, int highestCombo)
        {
            JudgementCounts = judgementCounts;
            JudgedNotesCount = judgedNotesCount;
            CurrentDiscreteAccuracy = currentDiscreteAccuracy;
            CurrentContinuousAccuracy = currentContinuousAccuracy;
            HighestCombo = highestCombo;
        }
    }
}
