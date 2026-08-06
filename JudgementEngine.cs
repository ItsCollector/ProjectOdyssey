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

            // Second chance - reholding long note to award a 'bad' - can be cancelled if released too early again
            else if (inputDirection == InputDirection.Down && noteState == NoteState.ReleasedEarly)
            {
                return (JudgementType.Bad, NoteState.Holding);
            }

            Debug.Fail($"Unreachable JudgeTail state: direction={inputDirection}, noteState={noteState}\nYou royally fucked up the placement of this function or you missed a case dumbass.");
            return (JudgementType.Miss, NoteState.Resolved); // Fallback for unexpected state
        }

        // Resolve overheld notes that have exceeded the tail time + 200ms threshold
        public static bool TryResolveOverheldNote(NoteState noteState, long tailTime, long now, out JudgementType result)
        {
            if (noteState == NoteState.Holding && now > tailTime + missWindowMs)
            {
                result = JudgementType.Miss;
                return true;
            }
            result = default;
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
