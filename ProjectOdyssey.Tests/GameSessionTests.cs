using ProjectOdyssey.Engine;
using ProjectOdyssey.Engine.Rulesets;
using ProjectOdyssey.Input;
using ProjectOdyssey.IO;

namespace ProjectOdyssey.Tests
{
    // Drives GameSession.JudgeNotes / HandleUnjudgedNotes directly (no gameplay thread),
    // checking that outcomes from the mechanics end up applied correctly to combo, cursors,
    // recorded results and accuracy.
    public class GameSessionTests
    {
        private const ushort KeyColumn0 = 83; // S
        private const ushort KeyColumn1 = 68; // D

        private static Note Tap(float time)
            => new() { NoteType = NoteType.Tap, StartTime = time, EndTime = time };

        private static Note Long(float start, float end)
            => new() { NoteType = NoteType.Long, StartTime = start, EndTime = end };

        private static GameSession CreateSession(Note[] column0, Note[]? column1 = null, IRuleset? ruleset = null)
        {
            var columns = new Note[7][];
            for (int i = 0; i < columns.Length; i++) columns[i] = Array.Empty<Note>();
            columns[0] = column0;
            if (column1 != null) columns[1] = column1;

            var chart = new ChartData("t", "a", "n", "d", 7, columns);
            return new GameSession(new InputHistory(), chart, ruleset);
        }

        private static void Press(GameSession session, ushort key, float time)
            => session.JudgeNotes(new InputEvent { VKey = key, IsPressed = true }, time);

        private static void Release(GameSession session, ushort key, float time)
            => session.JudgeNotes(new InputEvent { VKey = key, IsPressed = false }, time);

        [Fact]
        public void TapHit_IncrementsComboAdvancesCursorAndRecordsJudgement()
        {
            var session = CreateSession(new[] { Tap(1000) });

            Press(session, KeyColumn0, 1005);

            Assert.Equal(1, session.Combo);
            Assert.Equal(1, session.ColumnCursors[0]);
            Assert.Equal(JudgementType.Marvellous, session.CurrentJudgementResult.Type);
            Assert.Single(session.GetRecentJudgementResults());
        }

        [Fact]
        public void TapMissedByTime_BreaksComboAdvancesCursorAndRecordsMiss()
        {
            var session = CreateSession(new[] { Tap(1000) }, new[] { Tap(500) });
            Press(session, KeyColumn1, 500);
            Assert.Equal(1, session.Combo);

            session.HandleUnjudgedNotes(1201);

            Assert.Equal(0, session.Combo);
            Assert.Equal(1, session.ColumnCursors[0]);
            Assert.Equal(JudgementType.Miss, session.CurrentJudgementResult.Type);
        }

        [Fact]
        public void TapNotYetPastMissWindow_IsLeftAlone()
        {
            var session = CreateSession(new[] { Tap(1000) });

            session.HandleUnjudgedNotes(1200);

            Assert.Equal(0, session.ColumnCursors[0]);
            Assert.Empty(session.GetRecentJudgementResults());
        }

        [Fact]
        public void PressBeyondRulesetGhostThreshold_IsDropped()
        {
            var session = CreateSession(new[] { Tap(1000) }, ruleset: new NativeRuleset { GhostTapThreshold = 100f });

            Press(session, KeyColumn0, 850);

            Assert.Equal(0, session.Combo);
            Assert.Equal(0, session.ColumnCursors[0]);
            Assert.Empty(session.GetRecentJudgementResults());
        }

        [Fact]
        public void PressBetweenOldBadAndMissWindows_IsNowBadAndKeepsCombo()
        {
            var session = CreateSession(new[] { Tap(1000) });

            Press(session, KeyColumn0, 1180); // 180ms late: used to be a Miss that still incremented combo

            Assert.Equal(JudgementType.Bad, session.CurrentJudgementResult.Type);
            Assert.Equal(1, session.Combo);
        }

        [Fact]
        public void LongNote_EarlyRelease_BreaksComboWithoutRecordingAJudgementOrAdvancing()
        {
            var session = CreateSession(new[] { Long(1000, 2000) });
            Press(session, KeyColumn0, 1000);
            Assert.Equal(1, session.Combo);

            Release(session, KeyColumn0, 1500);

            Assert.Equal(0, session.Combo);
            Assert.Equal(0, session.ColumnCursors[0]);
            Assert.Equal(NoteState.ReleasedEarly, session.NotesByColumn[0][0].NoteState);
            Assert.Single(session.GetRecentJudgementResults()); // only the head
        }

        [Fact]
        public void LongNote_DuplicateReleaseWhileReleasedEarly_DoesNotBreakComboAgain()
        {
            var session = CreateSession(new[] { Long(1000, 2000) }, new[] { Tap(1600) });
            Press(session, KeyColumn0, 1000);
            Release(session, KeyColumn0, 1500);          // breaks combo
            Press(session, KeyColumn1, 1600);            // other column builds it back up
            Assert.Equal(1, session.Combo);

            Release(session, KeyColumn0, 1650);          // bounced release on the ReleasedEarly note

            Assert.Equal(1, session.Combo);
        }

        [Fact]
        public void LongNote_RecoveredTail_ResolvesAsBadAndAdvances()
        {
            var session = CreateSession(new[] { Long(1000, 2000) });
            Press(session, KeyColumn0, 1000);
            Release(session, KeyColumn0, 1500);
            Press(session, KeyColumn0, 1600);            // Recovering

            Release(session, KeyColumn0, 2000);

            Assert.Equal(1, session.ColumnCursors[0]);
            Assert.Equal(JudgementType.Bad, session.CurrentJudgementResult.Type);
            Assert.Equal(1, session.Combo);              // head +1, break, tail +1
        }

        [Fact]
        public void LongNote_CleanHold_RecordsHeadAndTailAndGivesTwoCombo()
        {
            var session = CreateSession(new[] { Long(1000, 2000) });

            Press(session, KeyColumn0, 1000);
            Release(session, KeyColumn0, 2000);

            Assert.Equal(2, session.Combo);
            Assert.Equal(1, session.ColumnCursors[0]);
            Assert.Equal(2, session.GetRecentJudgementResults().Length);
        }

        [Fact]
        public void UntouchedLongNote_RecordsHeadMissThenTailMiss()
        {
            var session = CreateSession(new[] { Long(1000, 2000) });

            session.HandleUnjudgedNotes(1201);
            Assert.Equal(0, session.ColumnCursors[0]);
            Assert.Single(session.GetRecentJudgementResults());

            session.HandleUnjudgedNotes(2201);
            Assert.Equal(1, session.ColumnCursors[0]);
            Assert.Equal(2, session.GetRecentJudgementResults().Length);
            Assert.All(session.GetRecentJudgementResults(), r => Assert.Equal(JudgementType.Miss, r.Type));
        }

        [Fact]
        public void Accuracy_StartsAtHundredAndDropsWithAMiss()
        {
            var session = CreateSession(new[] { Tap(1000), Tap(2000) });
            Assert.Equal(100f, session.Accuracy);

            Press(session, KeyColumn0, 1000);            // perfect: contributes 1.0
            Assert.Equal(100f, session.Accuracy);

            session.HandleUnjudgedNotes(2201);           // miss: contributes 0
            Assert.Equal(50f, session.Accuracy, 0.01f);
        }
    }
}
