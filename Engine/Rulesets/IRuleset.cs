using ProjectOdyssey.Engine.Mechanics;

namespace ProjectOdyssey.Engine.Rulesets
{
    public interface IRuleset
    {
        // Name of the ruleset
        string Name { get; set; }

        // Threshold where a key press isn't in any hit windows (notes not on screen) is not judged
        // Inputs further than this from the note being judged are dropped rather than judged.
        // Generally, just make this the same as your miss window unless you know what you are doing
        float GhostTapThreshold { get; set; }

        // Judgement windows, measured as an absolute deviation in ms from the note's time (inclusive upper edge).
        // Do NOT list Miss here: Miss is simply what happens outside the widest window,
        // and that widest window is what the mechanics treat as the miss window.
        List<Judgement> Judgements { get; set; }

        // Creates the mechanics (hit behaviour, LN rules) this ruleset uses.
        // Returns a fresh instance each call so no state can leak between play sessions.
        RulesetMechanics CreateMechanics();
    }

    public struct Judgement
    {
        public string Name;
        public float HitWindow;
        public JudgementType Type;
    }
}
