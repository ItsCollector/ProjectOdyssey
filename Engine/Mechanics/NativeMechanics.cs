using ProjectOdyssey.Engine.Rulesets;
using System.Diagnostics;

namespace ProjectOdyssey.Engine.Mechanics
{
    // The native mechanics:
    //  - Tap notes need a key press (down). The press is judged against the note's time.
    //  - Long notes give one judgement for the head (press) and one for the tail (release).
    //    Releasing early in the middle only breaks combo and flags the note ReleasedEarly.
    //    Pressing again moves it to Recovering, which allows a tail judgement capped at Bad.
    //  - Notes that scroll past the miss window give a Miss and break combo. An untouched
    //    long note gives a Miss for the head, then still allows a recovery for the tail.
    //  - Any Miss judgement breaks combo.
    //
    //  Note type / state                      Event                          Judgement      New state       Cursor    Combo
    //  Tap, Waiting                           press within ghost threshold   by deviation   Resolved        advance   +1 (Miss: break)
    //  Tap, Waiting                           past miss window               Miss           Resolved        advance   break
    //  LN, Waiting                            press within ghost threshold   head, by dev   Holding         stay      +1
    //                                         (head maps to Miss)            Miss           ReleasedEarly   stay      break
    //  LN, Waiting                            past miss window               head Miss      ReleasedEarly   stay      break
    //  LN, Holding                            release (not too early)        tail, by dev   Resolved        advance   +1 (Miss: break)
    //  LN, Holding                            release earlier than window    none           ReleasedEarly   stay      break
    //  LN, ReleasedEarly                      press                          none           Recovering      stay      none
    //  LN, Recovering                         release (not too early)        Bad (capped)   Resolved        advance   +1
    //  LN, Recovering                         release earlier than window    none           ReleasedEarly   stay      break
    //  LN, Holding/Recovering/ReleasedEarly   past tail + miss window        tail Miss      Resolved        advance   break
    //  Anything else                          none                           unchanged                      stay      none
    public class NativeMechanics : RulesetMechanics
    {
        public NativeMechanics(IRuleset ruleset) : base(ruleset) { }

        // Passive misses weren't pressed at all, so there is no real deviation.
        // Report one just past the miss window (the HUD/accuracy treat it as "worst possible").
        private float PassiveMissDeviation => MissWindow + 1f;

        public override MechanicsOutcome OnInput(Note note, InputDirection direction, float inputSongTimeMs)
        {
            if (note.NoteType == NoteType.Tap)
            {
                return TapInput(note, direction, inputSongTimeMs);
            }

            if (note.NoteType == NoteType.Long)
            {
                return note.NoteState == NoteState.Waiting
                    ? LongHeadInput(note, direction, inputSongTimeMs)
                    : LongTailInput(note, direction, inputSongTimeMs);
            }

            Debug.Fail($"[ERROR] Unreachable state because of incorrect NoteType passed: {note.NoteType}");
            return MechanicsOutcome.NoChange(note.NoteState);
        }

        public override MechanicsOutcome OnTick(Note note, float nowSongTimeMs)
        {
            if (note.NoteState == NoteState.Resolved)
            {
                return new MechanicsOutcome(null, 0f, NoteState.Resolved, true, ComboEffect.None);
            }

            if (note.NoteType == NoteType.Tap)
            {
                if (nowSongTimeMs - note.StartTime > MissWindow)
                {
                    return new MechanicsOutcome(JudgementType.Miss, PassiveMissDeviation, NoteState.Resolved, true, ComboEffect.Break);
                }

                return MechanicsOutcome.NoChange(note.NoteState);
            }

            if (note.NoteType == NoteType.Long)
            {
                // Head scrolled past the window unhit: Miss for the head, but the tail can still be recovered.
                if (note.NoteState == NoteState.Waiting)
                {
                    if (nowSongTimeMs - note.StartTime > MissWindow)
                    {
                        return new MechanicsOutcome(JudgementType.Miss, PassiveMissDeviation, NoteState.ReleasedEarly, false, ComboEffect.Break);
                    }

                    return MechanicsOutcome.NoChange(note.NoteState);
                }

                // Held (or released/recovering) past the tail + miss window: tail Miss.
                if (nowSongTimeMs - note.EndTime > MissWindow)
                {
                    return new MechanicsOutcome(JudgementType.Miss, PassiveMissDeviation, NoteState.Resolved, true, ComboEffect.Break);
                }

                return MechanicsOutcome.NoChange(note.NoteState);
            }

            Debug.Fail($"[ERROR] Unreachable state because of incorrect NoteType passed: {note.NoteType}");
            return MechanicsOutcome.NoChange(note.NoteState);
        }

        private MechanicsOutcome TapInput(Note note, InputDirection direction, float inputSongTimeMs)
        {
            if (direction != InputDirection.Down) return MechanicsOutcome.NoChange(note.NoteState);

            float hitDeviation = GetHitDeviation(inputSongTimeMs, note.StartTime);
            if (Math.Abs(hitDeviation) > GhostTapThreshold) return MechanicsOutcome.NoChange(note.NoteState);

            JudgementType judgement = MapDeltaToJudgement(hitDeviation);
            return new MechanicsOutcome(judgement, hitDeviation, NoteState.Resolved, true, ComboForJudgement(judgement));
        }

        private MechanicsOutcome LongHeadInput(Note note, InputDirection direction, float inputSongTimeMs)
        {
            if (direction != InputDirection.Down) return MechanicsOutcome.NoChange(note.NoteState);

            float hitDeviation = GetHitDeviation(inputSongTimeMs, note.StartTime);
            if (Math.Abs(hitDeviation) > GhostTapThreshold) return MechanicsOutcome.NoChange(note.NoteState);

            JudgementType judgement = MapDeltaToJudgement(hitDeviation);

            // A head that maps to Miss (only possible when the ghost threshold is wider than the miss window)
            // behaves like a head that scrolled past: break combo, tail stays recoverable.
            NoteState newState = judgement == JudgementType.Miss ? NoteState.ReleasedEarly : NoteState.Holding;
            return new MechanicsOutcome(judgement, hitDeviation, newState, false, ComboForJudgement(judgement));
        }

        private MechanicsOutcome LongTailInput(Note note, InputDirection direction, float inputSongTimeMs)
        {
            float hitDeviation = GetHitDeviation(inputSongTimeMs, note.EndTime);
            bool releasedTooEarly = hitDeviation < -MissWindow;

            // Released while held normally.
            if (direction == InputDirection.Up && note.NoteState == NoteState.Holding)
            {
                if (releasedTooEarly)
                {
                    return new MechanicsOutcome(null, hitDeviation, NoteState.ReleasedEarly, false, ComboEffect.Break);
                }

                JudgementType judgement = MapDeltaToJudgement(hitDeviation);
                return new MechanicsOutcome(judgement, hitDeviation, NoteState.Resolved, true, ComboForJudgement(judgement));
            }

            // Pressed again after an early release: enter recovery.
            if (direction == InputDirection.Down && note.NoteState == NoteState.ReleasedEarly)
            {
                return new MechanicsOutcome(null, hitDeviation, NoteState.Recovering, false, ComboEffect.None);
            }

            // Released while recovering. The judgement is capped at Bad regardless of timing,
            // since the player already let go of this note once.
            if (direction == InputDirection.Up && note.NoteState == NoteState.Recovering)
            {
                if (releasedTooEarly)
                {
                    return new MechanicsOutcome(null, hitDeviation, NoteState.ReleasedEarly, false, ComboEffect.Break);
                }

                return new MechanicsOutcome(JudgementType.Bad, hitDeviation, NoteState.Resolved, true, ComboEffect.Increment);
            }

            // Everything else carries no new information: a press while already holding/recovering,
            // or a duplicate release while already released early.
            return MechanicsOutcome.NoChange(note.NoteState);
        }

        private static ComboEffect ComboForJudgement(JudgementType judgement)
        {
            return judgement == JudgementType.Miss ? ComboEffect.Break : ComboEffect.Increment;
        }
    }
}
