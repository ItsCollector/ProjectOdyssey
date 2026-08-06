using Xunit;
using ProjectOdyssey;

namespace ProjectOdyssey.Tests
{
    public class JudgementEngineTests
    {
        // MapDeltaToJudgement / JudgeHead — boundary values for each tier
        [Theory]
        [InlineData(0, JudgementType.Marvellous)]
        [InlineData(20, JudgementType.Marvellous)]   // upper edge of Marvellous
        [InlineData(21, JudgementType.Perfect)]      // just past the edge
        [InlineData(50, JudgementType.Perfect)]
        [InlineData(51, JudgementType.Great)]
        [InlineData(100, JudgementType.Great)]
        [InlineData(101, JudgementType.Good)]
        [InlineData(150, JudgementType.Good)]
        [InlineData(151, JudgementType.Bad)]
        [InlineData(200, JudgementType.Bad)]
        [InlineData(201, JudgementType.Miss)]
        [InlineData(1000, JudgementType.Miss)]
        public void MapDeltaToJudgement_ReturnsExpectedTier(long delta, JudgementType expected)
        {
            var result = JudgementEngine.MapDeltaToJudgement(delta);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void JudgeHead_UsesAbsoluteDelta_EarlyAndLateHitsMatch()
        {
            // Hitting 30ms early and 30ms late should produce the same judgement,
            // since JudgeHead should be direction-agnostic (unlike JudgeTail).
            var early = JudgementEngine.JudgeHead(inputTimestamp: 970, nearestNoteTime: 1000);
            var late = JudgementEngine.JudgeHead(inputTimestamp: 1030, nearestNoteTime: 1000);

            Assert.Equal(JudgementType.Perfect, early);
            Assert.Equal(JudgementType.Perfect, late);
        }

        [Fact]
        public void JudgeHead_ExactHit_ReturnsMarvellous()
        {
            var result = JudgementEngine.JudgeHead(inputTimestamp: 5000, nearestNoteTime: 5000);
            Assert.Equal(JudgementType.Marvellous, result);
        }

        // JudgeTail — normal release within the judgeable window
        [Fact]
        public void JudgeTail_ReleaseExactlyOnTime_ReturnsMarvellousAndResolved()
        {
            var (judgement, state) = JudgementEngine.JudgeTail(
                inputTimestamp: 2000,
                nearestNoteTime: 2000,
                inputDirection: InputDirection.Up,
                noteState: NoteState.Holding);

            Assert.Equal(JudgementType.Marvellous, judgement);
            Assert.Equal(NoteState.Resolved, state);
        }

        [Fact]
        public void JudgeTail_ReleaseSlightlyEarly_WithinWindow_ResolvesNormally()
        {
            // 50ms early, well inside the -200ms early-release cutoff, should
            // resolve immediately via MapDeltaToJudgement rather than triggering
            // the ReleasedEarly second-chance path.
            var (judgement, state) = JudgementEngine.JudgeTail(
                inputTimestamp: 1950,
                nearestNoteTime: 2000,
                inputDirection: InputDirection.Up,
                noteState: NoteState.Holding);

            Assert.Equal(JudgementType.Perfect, judgement);
            Assert.Equal(NoteState.Resolved, state);
        }

        [Fact]
        public void JudgeTail_ReleaseSlightlyLate_WithinWindow_ResolvesNormally()
        {
            var (judgement, state) = JudgementEngine.JudgeTail(
                inputTimestamp: 2100,
                nearestNoteTime: 2000,
                inputDirection: InputDirection.Up,
                noteState: NoteState.Holding);

            Assert.Equal(JudgementType.Great, judgement);
            Assert.Equal(NoteState.Resolved, state);
        }

        [Fact]
        public void JudgeTail_ReleaseAtExactEarlyThreshold_StillResolvesNormally()
        {
            // signedDelta == -200 should NOT trigger ReleasedEarly, since the
            // condition is strictly "< earlyReleaseTolerance" (-200), not "<=".
            var (judgement, state) = JudgementEngine.JudgeTail(
                inputTimestamp: 1800,
                nearestNoteTime: 2000,
                inputDirection: InputDirection.Up,
                noteState: NoteState.Holding);

            Assert.Equal(NoteState.Resolved, state);
            Assert.NotEqual(NoteState.ReleasedEarly, state);
        }


        // JudgeTail — early release triggers the second-chance path
        [Fact]
        public void JudgeTail_ReleaseWayTooEarly_ReturnsMissAndReleasedEarly()
        {
            // 201ms early — just past the -200ms threshold.
            var (judgement, state) = JudgementEngine.JudgeTail(
                inputTimestamp: 1799,
                nearestNoteTime: 2000,
                inputDirection: InputDirection.Up,
                noteState: NoteState.Holding);

            Assert.Equal(JudgementType.Miss, judgement);
            Assert.Equal(NoteState.ReleasedEarly, state);
        }

        [Fact]
        public void JudgeTail_RepressAfterEarlyRelease_ReturnsBadAndHolding()
        {
            var (judgement, state) = JudgementEngine.JudgeTail(
                inputTimestamp: 1850,
                nearestNoteTime: 2000,
                inputDirection: InputDirection.Down,
                noteState: NoteState.ReleasedEarly);

            Assert.Equal(JudgementType.Bad, judgement);
            Assert.Equal(NoteState.Holding, state);
        }

        [Fact]
        public void JudgeTail_SecondEarlyReleaseAfterRepress_ReturnsMissAndReleasedEarlyAgain()
        {
            // Simulates: release early -> repress -> release early again.
            // The second early release should behave identically to the first.
            var (_, afterRepress) = JudgementEngine.JudgeTail(
                inputTimestamp: 1850,
                nearestNoteTime: 2000,
                inputDirection: InputDirection.Down,
                noteState: NoteState.ReleasedEarly);

            var (judgement, finalState) = JudgementEngine.JudgeTail(
                inputTimestamp: 1750,
                nearestNoteTime: 2000,
                inputDirection: InputDirection.Up,
                noteState: afterRepress);

            Assert.Equal(JudgementType.Miss, judgement);
            Assert.Equal(NoteState.ReleasedEarly, finalState);
        }

        // ----------------------------------------------------------------
        // TryResolveOverheldNote
        // ----------------------------------------------------------------

        [Fact]
        public void TryResolveOverheldNote_StillWithinWindow_ReturnsFalse()
        {
            bool resolved = JudgementEngine.TryResolveOverheldNote(
                noteState: NoteState.Holding,
                tailTime: 2000,
                now: 2199, // 199ms past tail, still within the 200ms threshold
                result: out var judgement);

            Assert.False(resolved);
            Assert.Equal(default(JudgementType), judgement);
        }

        [Fact]
        public void TryResolveOverheldNote_AtExactThreshold_ReturnsFalse()
        {
            // now == tailTime + 200 should NOT trigger, since the condition
            // is strictly "now > tailTime + 200".
            bool resolved = JudgementEngine.TryResolveOverheldNote(
                noteState: NoteState.Holding,
                tailTime: 2000,
                now: 2200,
                result: out _);

            Assert.False(resolved);
        }

        [Fact]
        public void TryResolveOverheldNote_PastThreshold_ReturnsTrueWithMiss()
        {
            bool resolved = JudgementEngine.TryResolveOverheldNote(
                noteState: NoteState.Holding,
                tailTime: 2000,
                now: 2201,
                result: out var judgement);

            Assert.True(resolved);
            Assert.Equal(JudgementType.Miss, judgement);
        }

        [Theory]
        [InlineData(NoteState.ReleasedEarly)]
        [InlineData(NoteState.Resolved)]
        public void TryResolveOverheldNote_NotHolding_NeverTriggersRegardlessOfTime(NoteState noteState)
        {
            // Even far past the threshold, a note that isn't actively Holding
            // should never be resolved by this method.
            bool resolved = JudgementEngine.TryResolveOverheldNote(
                noteState: noteState,
                tailTime: 2000,
                now: 100000,
                result: out _);

            Assert.False(resolved);
        }

        // Integration-style: a full LN lifecycle through the state machine

        [Fact]
        public void FullLifecycle_EarlyRelease_Repress_ThenCorrectRelease()
        {
            var state = NoteState.Holding;
            JudgementType judgement;

            // Player lets go way too early
            (judgement, state) = JudgementEngine.JudgeTail(1700, 2000, InputDirection.Up, state);
            Assert.Equal(NoteState.ReleasedEarly, state);
            Assert.Equal(JudgementType.Miss, judgement);

            // Player notices and represses
            (judgement, state) = JudgementEngine.JudgeTail(1750, 2000, InputDirection.Down, state);
            Assert.Equal(NoteState.Holding, state);
            Assert.Equal(JudgementType.Bad, judgement);

            // Player holds through and releases correctly at the tail
            (judgement, state) = JudgementEngine.JudgeTail(2005, 2000, InputDirection.Up, state);
            Assert.Equal(NoteState.Resolved, state);
            Assert.Equal(JudgementType.Marvellous, judgement);
            // Note: this overwrites the earlier "Bad" from the repress —
            // confirm this is the intended final-judgement behavior, since
            // it means the repress judgement is discarded, not combined.
        }

        [Fact]
        public void FullLifecycle_HeldPastTailWithNoRelease_ResolvesViaOverholdCheck()
        {
            var state = NoteState.Holding;

            // No key-up event ever fires — simulate the per-tick check instead.
            bool resolved = JudgementEngine.TryResolveOverheldNote(
                state, tailTime: 2000, now: 2500, out var judgement);

            Assert.True(resolved);
            Assert.Equal(JudgementType.Miss, judgement);
        }
    }
}