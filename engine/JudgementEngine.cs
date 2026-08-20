using System.Diagnostics;

namespace ProjectOdyssey
{
    public static class JudgementEngine
    {
        public const float earlyReleaseToleranceMs = -200; 
        public const float missWindowMs = 200;

        // Judge Tap notes or the head of Long Notes
        public static JudgementType JudgeHead(float inputTimestamp, float nearestNoteTime)
            => MapDeltaToJudgement(Math.Abs(inputTimestamp - nearestNoteTime));

        // Judge the tail of a Long Note, given the direction of the current input event
        // (key down/up) and the note's current hold state. Returns the resulting
        // judgement (if any) and the note's new state after processing this input.
        public static (JudgementType, NoteState) JudgeTail(float inputTimestamp, float nearestNoteTime, InputDirection inputDirection, NoteState noteState)
        {
            // Signed offset between when the input happened and when the tail was due.
            // Negative = input was early, positive = input was late.
            float signedDelta = inputTimestamp - nearestNoteTime;

            // Case 1: key released while the note is currently being held normally.
            if (inputDirection == InputDirection.Up && noteState == NoteState.Holding)
            {
                // Released too early to count as a valid tail hit at all -> miss,
                // and flag the note so a repress can attempt recovery.
                // Otherwise, released within a judgeable window -> map the timing
                // delta to a judgement and resolve the note normally.
                return signedDelta < earlyReleaseToleranceMs
                    ? (JudgementType.Miss, NoteState.ReleasedEarly)
                    : (MapDeltaToJudgement(Math.Abs(signedDelta)), NoteState.Resolved);
            }
            // Case 2: key pressed again after an early release -> enter recovery.
            // The judgement is capped at Bad regardless of timing, since the note
            // was already let go once.
            else if (inputDirection == InputDirection.Down && noteState == NoteState.ReleasedEarly)
            {
                return (JudgementType.Bad, NoteState.Recovering);
            }
            // Case 3: key released again while recovering from an earlier early release.
            else if (inputDirection == InputDirection.Up && noteState == NoteState.Recovering)
            {
                // Released early again -> back to ReleasedEarly, still eligible for
                // another recovery attempt.
                // Released within the window -> resolves, but judgement stays capped
                // at Bad since a clean hold was never achieved.
                return signedDelta < earlyReleaseToleranceMs
                    ? (JudgementType.Miss, NoteState.ReleasedEarly)
                    : (JudgementType.Bad, NoteState.Resolved);
            }
            // Case 4: duplicate/bounced release event while already ReleasedEarly
            // (no corresponding press happened in between) -> no new information,
            // stay in ReleasedEarly.
            else if (inputDirection == InputDirection.Up && noteState == NoteState.ReleasedEarly)
            {
                return (JudgementType.Miss, NoteState.ReleasedEarly);
            }
            // Case 5: a key-down while already Holding or Recovering (e.g. input
            // bounce/repeat) carries no new information -> ignore, state unchanged.
            else if (inputDirection == InputDirection.Down)
            {
                return (default, noteState);
            }

            // Should never be reached — every valid (direction, state) combination
            // is handled above. If this fires, a new state or input case was added
            // without updating this function.
            Debug.Fail($"Unreachable JudgeTail state: direction={inputDirection}, noteState={noteState}");
            return (JudgementType.Miss, NoteState.Resolved);
        }

        // Resolve overheld notes or non-held nones that have exceeded the tail time + 200ms threshold
        public static bool TryResolveOverheldNote(NoteState noteState, float tailTime, float now, out JudgementType result, out NoteState newState)
        {
            if ((noteState == NoteState.Holding || noteState == NoteState.Recovering || noteState == NoteState.ReleasedEarly) && now > tailTime + missWindowMs)
            {
                result = JudgementType.Miss;
                newState = NoteState.Resolved;
                return true;
            }

            result = default;
            newState = noteState; // unchanged
            return false;
        }

        // Map the absolute delta between input and note time to a JudgementType
        public static JudgementType MapDeltaToJudgement(float delta)
        {
            if (delta <= 20)
                return JudgementType.Marvellous;
            else if (delta <= 50)
                return JudgementType.Perfect;
            else if (delta <= 100)
                return JudgementType.Great;
            else if (delta <= 150)
                return JudgementType.Good;
            else if (delta <= 200)
                return JudgementType.Bad;
            else
                return JudgementType.Miss;
        }
    }
}
