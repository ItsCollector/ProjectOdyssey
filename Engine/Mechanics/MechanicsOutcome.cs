namespace ProjectOdyssey.Engine.Mechanics
{
    public enum ComboEffect
    {
        None,
        Increment,
        Break
    }

    // Everything that follows from one input event / tick for one note.
    // The mechanics decide this once; GameSession applies it without interpreting why.
    public readonly struct MechanicsOutcome
    {
        // The judgement to record (buffer, HUD, accuracy). Null = nothing is recorded.
        public JudgementType? Judgement { get; }

        // Signed deviation (ms) for the recorded judgement. Ignored when Judgement is null.
        public float HitDeviation { get; }

        // The note's state after this event (equal to the current state if nothing changed).
        public NoteState NewState { get; }

        // True when the column cursor should move past this note.
        public bool AdvanceCursor { get; }

        public ComboEffect Combo { get; }

        public MechanicsOutcome(JudgementType? judgement, float hitDeviation, NoteState newState, bool advanceCursor, ComboEffect combo)
        {
            Judgement = judgement;
            HitDeviation = hitDeviation;
            NewState = newState;
            AdvanceCursor = advanceCursor;
            Combo = combo;
        }

        // The event was ignored: no judgement, no state change, cursor and combo untouched.
        public static MechanicsOutcome NoChange(NoteState currentState)
            => new(null, 0f, currentState, false, ComboEffect.None);
    }
}
