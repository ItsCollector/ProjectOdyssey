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
            Assert.Equal(NoteState.Recovering, state);
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

        // TryResolveOverheldNote

        [Fact]
        public void TryResolveOverheldNote_ReleasedEarlyPastThreshold_ReturnsTrue()
        {
            bool resolved = JudgementEngine.TryResolveOverheldNote(
                noteState: NoteState.ReleasedEarly,
                tailTime: 2000,
                now: 100000,
                result: out var judgement,
                newState: out var finalState);

            Assert.True(resolved);
            Assert.Equal(JudgementType.Miss, judgement);
            Assert.Equal(NoteState.Resolved, finalState);
        }

        [Fact]
        public void TryResolveOverheldNote_AlreadyResolved_NeverTriggersRegardlessOfTime()
        {
            // Resolved notes are done — this should genuinely never fire again.
            bool resolved = JudgementEngine.TryResolveOverheldNote(
                noteState: NoteState.Resolved,
                tailTime: 2000,
                now: 100000,
                result: out _,
                newState: out _);

            Assert.False(resolved);
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
                result: out _,
                newState: out _);

            Assert.False(resolved);
        }

        [Fact]
        public void TryResolveOverheldNote_PastThreshold_ReturnsTrueWithMiss()
        {
            bool resolved = JudgementEngine.TryResolveOverheldNote(
                noteState: NoteState.Holding,
                tailTime: 2000,
                now: 2201,
                result: out var judgement,
                newState: out var finalState);

            Assert.True(resolved);
            Assert.Equal(JudgementType.Miss, judgement);
            Assert.Equal(NoteState.Resolved, finalState);
        }

        // Integration-style: a full LN lifecycle through the state machine

        [Fact]
        public void FullLifecycle_HeldPastTailWithNoRelease_ResolvesViaOverholdCheck()
        {
            var state = NoteState.Holding;

            // No key-up event ever fires — simulate the per-tick check instead.
            bool resolved = JudgementEngine.TryResolveOverheldNote(
                noteState: state, 
                tailTime: 2000, 
                now: 2500, 
                result: out var judgement,
                newState: out var finalState);

            Assert.True(resolved);
            Assert.Equal(JudgementType.Miss, judgement);
            Assert.Equal(NoteState.Resolved, finalState);
        }

        // Release early -> repress -> release early again ->
        // repress again -> correct release at the tail.
        // Two mid-LN releases, one correct final release -> should still
        // be capped at Bad, not upgraded by the well-timed final release.
        [Fact]
        public void DoubleEarlyRelease_ThenCorrectFinalRelease_LocksToBad()
        {
            var state = NoteState.Holding;
            JudgementType judgement;

            // First early release
            (judgement, state) = JudgementEngine.JudgeTail(1500, 2000, InputDirection.Up, state);
            Assert.Equal(NoteState.ReleasedEarly, state);
            Assert.Equal(JudgementType.Miss, judgement);

            // First repress -> enters Recovering, capped at Bad
            (judgement, state) = JudgementEngine.JudgeTail(1650, 2000, InputDirection.Down, state);
            Assert.Equal(NoteState.Recovering, state);
            Assert.Equal(JudgementType.Bad, judgement);

            // Second early release, this time from Recovering -> back to ReleasedEarly
            (judgement, state) = JudgementEngine.JudgeTail(1700, 2000, InputDirection.Up, state);
            Assert.Equal(NoteState.ReleasedEarly, state);
            Assert.Equal(JudgementType.Miss, judgement);

            // Second repress -> Recovering again, still capped at Bad
            (judgement, state) = JudgementEngine.JudgeTail(1750, 2000, InputDirection.Down, state);
            Assert.Equal(NoteState.Recovering, state);
            Assert.Equal(JudgementType.Bad, judgement);

            // Correct, well-timed final release at the tail -- should NOT
            // upgrade past Bad despite hitting the tail exactly.
            (judgement, state) = JudgementEngine.JudgeTail(2000, 2000, InputDirection.Up, state);
            Assert.Equal(NoteState.Resolved, state);
            Assert.Equal(JudgementType.Bad, judgement);
        }

        // Release early -> repress -> hold straight through,
        // never releasing at all -> overhold timeout should fire a Miss,
        // not leave the note unresolved.
        [Fact]
        public void EarlyRelease_Repress_ThenHeldPastTailWithNoRelease_ResolvesMissViaOverholdCheck()
        {
            var state = NoteState.Holding;
            JudgementType judgement;

            (judgement, state) = JudgementEngine.JudgeTail(1700, 2000, InputDirection.Up, state);
            Assert.Equal(NoteState.ReleasedEarly, state);

            (judgement, state) = JudgementEngine.JudgeTail(1750, 2000, InputDirection.Down, state);
            Assert.Equal(NoteState.Recovering, state);
            Assert.Equal(JudgementType.Bad, judgement);

            // No further key event ever fires -- simulate the per-tick
            // overhold check instead, well past the tail + tolerance.
            bool resolved = JudgementEngine.TryResolveOverheldNote(
                noteState: state,
                tailTime: 2000,
                now: 2500,
                result: out var overheldJudgement,
                newState: out var finalState);

            Assert.True(resolved);
            Assert.Equal(JudgementType.Miss, overheldJudgement);
            Assert.Equal(NoteState.Resolved, finalState);
        }

        // Release early -> repress -> release early again ->
        // never reholds -> the grace window after the second early
        // release should expire into a Miss, not silently hang.
        [Fact]
        public void EarlyRelease_Repress_SecondEarlyRelease_NeverRepressedAgain_ResolvesMiss()
        {
            var state = NoteState.Holding;
            JudgementType judgement;

            (judgement, state) = JudgementEngine.JudgeTail(1500, 2000, InputDirection.Up, state);
            Assert.Equal(NoteState.ReleasedEarly, state);

            (judgement, state) = JudgementEngine.JudgeTail(1650, 2000, InputDirection.Down, state);
            Assert.Equal(NoteState.Recovering, state);

            (judgement, state) = JudgementEngine.JudgeTail(1750, 2000, InputDirection.Up, state);
            Assert.Equal(NoteState.ReleasedEarly, state);
            Assert.Equal(JudgementType.Miss, judgement);

            // Player never represses again. Nothing further calls JudgeTail,
            // since there's no more key event. This state needs to be
            // resolved by time passing, same as the overhold case, but
            // from ReleasedEarly rather than Holding/Recovering.
            bool resolved = JudgementEngine.TryResolveOverheldNote(
                noteState: state,
                tailTime: 2000,
                now: 2500,
                result: out var finalJudgement,
                newState: out var finalState);

            Assert.True(resolved);
            Assert.Equal(JudgementType.Miss, finalJudgement);
            Assert.Equal(NoteState.Resolved, finalState);
        }

    }
}