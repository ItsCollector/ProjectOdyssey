using ProjectOdyssey.Engine.Mechanics;

namespace ProjectOdyssey.Engine.Rulesets
{
    public class NativeRuleset : IRuleset
    {
        public string Name { get; set; } = "Standard";
        public float GhostTapThreshold { get; set; } = 200f;

        // Miss is deliberately not listed: anything beyond the widest window (Bad, 200ms) is a Miss.
        public List<Judgement> Judgements { get; set; } = new()
        {
            new Judgement { Name = "Marvellous", HitWindow = 16f,  Type = JudgementType.Marvellous },
            new Judgement { Name = "Perfect", HitWindow = 32f,  Type = JudgementType.Perfect },
            new Judgement { Name = "Great", HitWindow = 64f,  Type = JudgementType.Great },
            new Judgement { Name = "Good", HitWindow = 128f, Type = JudgementType.Good },
            new Judgement { Name = "Bad", HitWindow = 200f, Type = JudgementType.Bad },
        };

        public RulesetMechanics CreateMechanics() => new NativeMechanics(this);
    }
}
