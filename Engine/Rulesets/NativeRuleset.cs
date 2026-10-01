using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Engine.Rulesets
{
    public class NativeRuleset : IRuleset
    {
        public string Name { get; set; } = "Standard";
        public float GhostTapThreshold { get; set; } = 200f;
        public JudgementMechanics JudgementMechanics { get; set; } = JudgementMechanics.Native;

        public List<Judgement> Judgements { get; set; } = new()
        {
            new Judgement
            {
                Name = "Marvellous",
                HitWindow = 16f,
                Type = JudgementType.Marvellous
            },
            new Judgement
            {
                Name = "Marvellous",
                HitWindow = 32f,
                Type = JudgementType.Perfect
            },
            new Judgement
            {
                Name = "Marvellous",
                HitWindow = 64f,
                Type = JudgementType.Great
            },
            new Judgement
            {
                Name = "Marvellous",
                HitWindow = 128f,
                Type = JudgementType.Good
            },
            new Judgement
            {
                Name = "Marvellous",
                HitWindow = 160f,
                Type = JudgementType.Bad
            },
            new Judgement
            {
                Name = "Marvellous",
                HitWindow = 200f,
                Type = JudgementType.Miss
            },
        };
    }
}

