using ProjectOdyssey.Engine.Rulesets;

namespace ProjectOdyssey.Engine.Mechanics
{
    // Base class for a ruleset's mechanics: how inputs and the passage of time turn into judgements.
    // GameSession keeps note scrolling, the cursors, combo, accuracy and result recording;
    // a mechanics class only has to answer "what happened to this note?" via OnInput and OnTick.
    //
    // To add a new mechanics set (e.g. o2jam): extend this class, implement the two hooks,
    // and return an instance from your IRuleset.CreateMechanics().
    public abstract class RulesetMechanics
    {
        // Judgement windows sorted tightest first. Never contains Miss.
        private readonly Judgement[] windows;

        // The widest hit window. Anything beyond this deviation is a Miss.
        public float MissWindow { get; }

        // Inputs further than this from the note being judged are dropped (see IRuleset).
        public float GhostTapThreshold { get; }

        protected RulesetMechanics(IRuleset ruleset)
        {
            windows = ruleset.Judgements
                .Where(judgement => judgement.Type != JudgementType.Miss)
                .OrderBy(judgement => judgement.HitWindow)
                .ToArray();

            if (windows.Length == 0)
            {
                throw new ArgumentException($"Ruleset '{ruleset.Name}' defines no judgement windows.", nameof(ruleset));
            }

            MissWindow = windows[^1].HitWindow;
            GhostTapThreshold = ruleset.GhostTapThreshold;
        }

        // A key went down or up. `note` is the note at the front of that key's column.
        // Called on the gameplay thread only.
        public abstract MechanicsOutcome OnInput(Note note, InputDirection direction, float inputSongTimeMs);

        // Time passed with no input for this note. Handles notes that were never hit,
        // held too long, etc. `note` is the note at the front of its column.
        // Called on the gameplay thread only.
        public abstract MechanicsOutcome OnTick(Note note, float nowSongTimeMs);

        // Map a deviation (sign ignored) to the tightest judgement whose window contains it.
        public JudgementType MapDeltaToJudgement(float delta)
        {
            float absoluteDelta = Math.Abs(delta);

            foreach (var judgement in windows)
            {
                if (absoluteDelta <= judgement.HitWindow)
                {
                    return judgement.Type;
                }
            }

            return JudgementType.Miss;
        }

        // Negative = input was early, positive = input was late.
        public static float GetHitDeviation(float inputTimestamp, float noteTime)
        {
            return inputTimestamp - noteTime;
        }
    }
}
