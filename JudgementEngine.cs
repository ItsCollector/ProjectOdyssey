using System.Diagnostics;

namespace ProjectOdyssey
{
    public static class JudgementEngine
    {
        public const long earlyReleaseToleranceMs = -200; 
        public const long missWindowMs = 200;

        // Judge Tap notes or the head of Long Notes
        public static JudgementType JudgeHead(long inputTimestamp, long nearestNoteTime)
            => MapDeltaToJudgement(Math.Abs(inputTimestamp - nearestNoteTime));

        // Judge the tail of Long Notes
        public static (JudgementType, NoteState) JudgeTail(long inputTimestamp, long nearestNoteTime, InputDirection inputDirection, NoteState noteState)
        {
            long signedDelta = inputTimestamp - nearestNoteTime; // negative = early, positive = late

            // Key is released while the note is still being held
            if (inputDirection == InputDirection.Up && noteState == NoteState.Holding)
            {
                // released during LN body outside of early threshold
                if (signedDelta < earlyReleaseToleranceMs)
                {
                    return (JudgementType.Miss, NoteState.ReleasedEarly);
                }
                // released within the normal judgeable tail window 
                else
                {
                    return (MapDeltaToJudgement(Math.Abs(signedDelta)), NoteState.Resolved);
                }
            }
            // Repress after early release — enter recovery, capped outcome
            else if (inputDirection == InputDirection.Down && noteState == NoteState.ReleasedEarly)
            {
                return (JudgementType.Bad, NoteState.Recovering);
            }
            // Released again while recovering — early release from recovery goes back to ReleasedEarly,
            // eligible for another repress, but the eventual cap stays Bad either way
            else if (inputDirection == InputDirection.Up && noteState == NoteState.Recovering)
            {
                if (signedDelta < earlyReleaseToleranceMs)
                {
                    return (JudgementType.Miss, NoteState.ReleasedEarly);
                }
                else
                {
                    return (JudgementType.Bad, NoteState.Resolved); // locked, regardless of how well-timed
                }
            }
            else if (inputDirection == InputDirection.Up && noteState == NoteState.ReleasedEarly)
            {
                // Duplicate/bounced release event — no new information
                return (JudgementType.Miss, NoteState.ReleasedEarly);
            }

            Debug.Fail($"Unreachable JudgeTail state: direction={inputDirection}, noteState={noteState}\nYou royally fucked up the placement of this function or you missed a case dumbass.");
            return (JudgementType.Miss, NoteState.Resolved); // Fallback for unexpected state
        }

        // Resolve overheld notes or non-held nones that have exceeded the tail time + 200ms threshold
        public static bool TryResolveOverheldNote(NoteState noteState, long tailTime, long now, out JudgementType result, out NoteState newState)
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
        public static JudgementType MapDeltaToJudgement(long delta)
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
