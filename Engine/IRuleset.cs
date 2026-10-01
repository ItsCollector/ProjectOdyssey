using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Engine
{
    public interface IRuleset
    {
        // Name of the ruleset
        string Name { get; set; }

        // Threshold where a key press isn't in any hit windows (notes not on screen) is not judged
        // Generally, just make this the same as your miss window unless you know what you are doing
        float GhostTapThreshold { get; set; } 

        // Mechanics (hit behaviour, LN mechanics) that the ruleset uses
        JudgementMechanics JudgementMechanics { get; set; }

        // Judgement windows
        List<Judgement> Judgements { get; set; }
    }

    public enum JudgementMechanics
    {
        Native,
        o2jam
    }

    public struct Judgement
    {
        public string Name;
        public float HitWindow;
        public JudgementType Type;
    }
}
