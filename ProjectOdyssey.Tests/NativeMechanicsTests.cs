using ProjectOdyssey.Engine;
using ProjectOdyssey.Engine.Mechanics;
using ProjectOdyssey.Engine.Rulesets;

namespace ProjectOdyssey.Tests
{
    public class NativeMechanicsTests
    {
        private static NativeMechanics Create(IRuleset? ruleset = null)
            => new(ruleset ?? new NativeRuleset());

        private static Note Tap(float time)
            => new() { NoteType = NoteType.Tap, StartTime = time, EndTime = time };

        private static Note Long(float start, float end, NoteState state = NoteState.Waiting)
            => new() { NoteType = NoteType.Long, StartTime = start, EndTime = end, NoteState = state };

        // Apply an outcome to a note the way GameSession does, so multi-step lifecycles can be tested.
        private static MechanicsOutcome Apply(Note note, MechanicsOutcome outcome)
        {
            note.NoteState = outcome.NewState;
            return outcome;
        }

        private static void AssertNoChange(MechanicsOutcome outcome, NoteState expectedState)
        {
            Assert.Null(outcome.Judgement);
            Assert.Equal(expectedState, outcome.NewState);
            Assert.False(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.None, outcome.Combo);
        }

        // ---------------------------------------------------------------
        // Windows
        // ---------------------------------------------------------------

        [Theory]
        [InlineData(0f, JudgementType.Marvellous)]
        [InlineData(16f, JudgementType.Marvellous)]     // upper edge, inclusive
        [InlineData(16.5f, JudgementType.Perfect)]
        [InlineData(32f, JudgementType.Perfect)]
        [InlineData(32.5f, JudgementType.Great)]
        [InlineData(64f, JudgementType.Great)]
        [InlineData(64.5f, JudgementType.Good)]
        [InlineData(128f, JudgementType.Good)]
        [InlineData(128.5f, JudgementType.Bad)]
        [InlineData(160f, JudgementType.Bad)]           // used to be a Miss
        [InlineData(200f, JudgementType.Bad)]           // inclusive
        [InlineData(200.5f, JudgementType.Miss)]
        [InlineData(201f, JudgementType.Miss)]
        [InlineData(1000f, JudgementType.Miss)]
        public void MapDeltaToJudgement_ReturnsExpectedTier(float delta, JudgementType expected)
        {
            Assert.Equal(expected, Create().MapDeltaToJudgement(delta));
        }

        [Theory]
        [InlineData(-16f, JudgementType.Marvellous)]
        [InlineData(-17f, JudgementType.Perfect)]
        [InlineData(-200f, JudgementType.Bad)]
        [InlineData(-201f, JudgementType.Miss)]
        public void MapDeltaToJudgement_IgnoresSign(float delta, JudgementType expected)
        {
            Assert.Equal(expected, Create().MapDeltaToJudgement(delta));
        }

        [Fact]
        public void MissWindow_IsTheWidestWindow()
        {
            Assert.Equal(200f, Create().MissWindow);
        }

        [Fact]
        public void MissEntriesInRuleset_AreIgnored()
        {
            var ruleset = new NativeRuleset();
            ruleset.Judgements.Add(new Judgement { Name = "Miss", HitWindow = 500f, Type = JudgementType.Miss });

            var mechanics = Create(ruleset);

            Assert.Equal(200f, mechanics.MissWindow);
            Assert.Equal(JudgementType.Miss, mechanics.MapDeltaToJudgement(201f));
        }

        [Fact]
        public void RulesetWithNoWindows_Throws()
        {
            var ruleset = new NativeRuleset { Judgements = new List<Judgement>() };

            Assert.Throws<ArgumentException>(() => Create(ruleset));
        }

        [Fact]
        public void CreateMechanics_ReturnsFreshInstanceEachTime()
        {
            var ruleset = new NativeRuleset();

            Assert.NotSame(ruleset.CreateMechanics(), ruleset.CreateMechanics());
        }

        // ---------------------------------------------------------------
        // Tap notes
        // ---------------------------------------------------------------

        [Fact]
        public void Tap_PressOnTime_IsMarvellousResolvedAdvancesAndIncrementsCombo()
        {
            var outcome = Create().OnInput(Tap(1000), InputDirection.Down, 1000);

            Assert.Equal(JudgementType.Marvellous, outcome.Judgement);
            Assert.Equal(0f, outcome.HitDeviation);
            Assert.Equal(NoteState.Resolved, outcome.NewState);
            Assert.True(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Increment, outcome.Combo);
        }

        [Fact]
        public void Tap_EarlyAndLatePressesJudgeTheSame_WithSignedDeviation()
        {
            var early = Create().OnInput(Tap(1000), InputDirection.Down, 970);
            var late = Create().OnInput(Tap(1000), InputDirection.Down, 1030);

            Assert.Equal(early.Judgement, late.Judgement);
            Assert.Equal(-30f, early.HitDeviation);   // negative = early
            Assert.Equal(30f, late.HitDeviation);     // positive = late
        }

        [Fact]
        public void Tap_Release_IsIgnored()
        {
            var note = Tap(1000);

            AssertNoChange(Create().OnInput(note, InputDirection.Up, 1000), NoteState.Waiting);
        }

        [Fact]
        public void Tap_PressAtExactlyGhostThreshold_IsJudgedAsBad()
        {
            var outcome = Create().OnInput(Tap(1000), InputDirection.Down, 800);

            Assert.Equal(JudgementType.Bad, outcome.Judgement);
            Assert.Equal(ComboEffect.Increment, outcome.Combo);
        }

        [Fact]
        public void Tap_PressBeyondGhostThreshold_IsDropped()
        {
            AssertNoChange(Create().OnInput(Tap(1000), InputDirection.Down, 799), NoteState.Waiting);
        }

        [Fact]
        public void Tap_PressBetweenMissWindowAndWiderGhostThreshold_IsMissAndBreaksCombo()
        {
            var mechanics = Create(new NativeRuleset { GhostTapThreshold = 300f });

            var outcome = mechanics.OnInput(Tap(1000), InputDirection.Down, 750);

            Assert.Equal(JudgementType.Miss, outcome.Judgement);
            Assert.Equal(NoteState.Resolved, outcome.NewState);
            Assert.True(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Break, outcome.Combo);
        }

        [Fact]
        public void Tap_GhostThresholdComesFromTheRuleset()
        {
            var strict = Create(new NativeRuleset { GhostTapThreshold = 100f });

            AssertNoChange(strict.OnInput(Tap(1000), InputDirection.Down, 850), NoteState.Waiting);
            Assert.Equal(JudgementType.Bad, strict.MapDeltaToJudgement(150f)); // windows unaffected
        }

        [Fact]
        public void Tap_TickAtMissWindowEdge_DoesNothing()
        {
            AssertNoChange(Create().OnTick(Tap(1000), 1200), NoteState.Waiting);
        }

        [Fact]
        public void Tap_TickPastMissWindow_IsMissResolvedAdvancesAndBreaksCombo()
        {
            var outcome = Create().OnTick(Tap(1000), 1201);

            Assert.Equal(JudgementType.Miss, outcome.Judgement);
            Assert.True(outcome.HitDeviation > 200f);       // sentinel just past the miss window
            Assert.Equal(NoteState.Resolved, outcome.NewState);
            Assert.True(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Break, outcome.Combo);
        }

        [Fact]
        public void Tick_OnAlreadyResolvedNote_JustAdvancesTheCursor()
        {
            var note = Tap(1000);
            note.NoteState = NoteState.Resolved;

            var outcome = Create().OnTick(note, 1000);

            Assert.Null(outcome.Judgement);
            Assert.True(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.None, outcome.Combo);
        }

        // ---------------------------------------------------------------
        // Long note heads
        // ---------------------------------------------------------------

        [Fact]
        public void LongHead_PressInWindow_StartsHoldingAndStaysOnNote()
        {
            var outcome = Create().OnInput(Long(1000, 2000), InputDirection.Down, 1010);

            Assert.Equal(JudgementType.Marvellous, outcome.Judgement);
            Assert.Equal(NoteState.Holding, outcome.NewState);
            Assert.False(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Increment, outcome.Combo);
        }

        [Fact]
        public void LongHead_Release_IsIgnored()
        {
            AssertNoChange(Create().OnInput(Long(1000, 2000), InputDirection.Up, 1000), NoteState.Waiting);
        }

        [Fact]
        public void LongHead_PressBeyondGhostThreshold_IsDropped()
        {
            AssertNoChange(Create().OnInput(Long(1000, 2000), InputDirection.Down, 700), NoteState.Waiting);
        }

        [Fact]
        public void LongHead_PressThatMapsToMiss_BreaksComboAndLeavesTailRecoverable()
        {
            var mechanics = Create(new NativeRuleset { GhostTapThreshold = 300f });

            var outcome = mechanics.OnInput(Long(1000, 2000), InputDirection.Down, 750);

            Assert.Equal(JudgementType.Miss, outcome.Judgement);
            Assert.Equal(NoteState.ReleasedEarly, outcome.NewState);
            Assert.False(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Break, outcome.Combo);
        }

        [Fact]
        public void LongHead_TickPastMissWindow_IsHeadMissAndStaysOnNote()
        {
            var outcome = Create().OnTick(Long(1000, 2000), 1201);

            Assert.Equal(JudgementType.Miss, outcome.Judgement);
            Assert.Equal(NoteState.ReleasedEarly, outcome.NewState);
            Assert.False(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Break, outcome.Combo);
        }

        [Fact]
        public void LongHead_TickAtMissWindowEdge_DoesNothing()
        {
            AssertNoChange(Create().OnTick(Long(1000, 2000), 1200), NoteState.Waiting);
        }

        // ---------------------------------------------------------------
        // Long note tails
        // ---------------------------------------------------------------

        [Fact]
        public void LongTail_ReleaseOnTime_IsMarvellousResolvedAndAdvances()
        {
            var outcome = Create().OnInput(Long(1000, 2000, NoteState.Holding), InputDirection.Up, 2000);

            Assert.Equal(JudgementType.Marvellous, outcome.Judgement);
            Assert.Equal(NoteState.Resolved, outcome.NewState);
            Assert.True(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Increment, outcome.Combo);
        }

        [Theory]
        [InlineData(1950f, JudgementType.Great)]   // 50ms early
        [InlineData(2100f, JudgementType.Good)]    // 100ms late
        [InlineData(1800f, JudgementType.Bad)]     // exactly at the early cutoff, still resolves
        public void LongTail_ReleaseWithinWindow_ResolvesByDeviation(float releaseTime, JudgementType expected)
        {
            var outcome = Create().OnInput(Long(1000, 2000, NoteState.Holding), InputDirection.Up, releaseTime);

            Assert.Equal(expected, outcome.Judgement);
            Assert.Equal(NoteState.Resolved, outcome.NewState);
            Assert.True(outcome.AdvanceCursor);
        }

        [Fact]
        public void LongTail_ReleaseTooEarly_OnlyBreaksComboAndFlagsReleasedEarly()
        {
            var outcome = Create().OnInput(Long(1000, 2000, NoteState.Holding), InputDirection.Up, 1799);

            AssertReleasedEarly(outcome);
        }

        [Fact]
        public void LongTail_ReleaseLateBeyondWindowBeforeTickRuns_IsMissAndBreaksCombo()
        {
            var outcome = Create().OnInput(Long(1000, 2000, NoteState.Holding), InputDirection.Up, 2250);

            Assert.Equal(JudgementType.Miss, outcome.Judgement);
            Assert.Equal(NoteState.Resolved, outcome.NewState);
            Assert.True(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Break, outcome.Combo);
        }

        [Fact]
        public void LongTail_PressWhileHolding_IsIgnored()
        {
            AssertNoChange(Create().OnInput(Long(1000, 2000, NoteState.Holding), InputDirection.Down, 1500), NoteState.Holding);
        }

        [Fact]
        public void LongTail_PressAfterEarlyRelease_EntersRecoveringWithNoJudgementOrComboChange()
        {
            var outcome = Create().OnInput(Long(1000, 2000, NoteState.ReleasedEarly), InputDirection.Down, 1650);

            AssertNoChange(outcome, NoteState.Recovering);
        }

        [Fact]
        public void LongTail_DuplicateReleaseWhileReleasedEarly_IsIgnored()
        {
            AssertNoChange(Create().OnInput(Long(1000, 2000, NoteState.ReleasedEarly), InputDirection.Up, 1700), NoteState.ReleasedEarly);
        }

        [Fact]
        public void LongTail_ReleaseWhileRecovering_IsCappedAtBadEvenIfPerfectlyTimed()
        {
            var outcome = Create().OnInput(Long(1000, 2000, NoteState.Recovering), InputDirection.Up, 2000);

            Assert.Equal(JudgementType.Bad, outcome.Judgement);
            Assert.Equal(NoteState.Resolved, outcome.NewState);
            Assert.True(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Increment, outcome.Combo);
        }

        [Fact]
        public void LongTail_EarlyReleaseWhileRecovering_BreaksComboAndReturnsToReleasedEarly()
        {
            var outcome = Create().OnInput(Long(1000, 2000, NoteState.Recovering), InputDirection.Up, 1500);

            AssertReleasedEarly(outcome);
        }

        [Fact]
        public void LongTail_PressWhileRecovering_IsIgnored()
        {
            AssertNoChange(Create().OnInput(Long(1000, 2000, NoteState.Recovering), InputDirection.Down, 1700), NoteState.Recovering);
        }

        private static void AssertReleasedEarly(MechanicsOutcome outcome)
        {
            Assert.Null(outcome.Judgement);                 // no judgement is recorded for a mid-note release
            Assert.Equal(NoteState.ReleasedEarly, outcome.NewState);
            Assert.False(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Break, outcome.Combo);
        }

        // ---------------------------------------------------------------
        // Long note timeouts (held / released past the tail)
        // ---------------------------------------------------------------

        [Theory]
        [InlineData(NoteState.Holding)]
        [InlineData(NoteState.Recovering)]
        [InlineData(NoteState.ReleasedEarly)]
        public void LongTail_TickAtTailPlusMissWindow_DoesNothing(NoteState state)
        {
            AssertNoChange(Create().OnTick(Long(1000, 2000, state), 2200), state);
        }

        [Theory]
        [InlineData(NoteState.Holding)]
        [InlineData(NoteState.Recovering)]
        [InlineData(NoteState.ReleasedEarly)]
        public void LongTail_TickPastTailPlusMissWindow_IsTailMissResolvedAndBreaksCombo(NoteState state)
        {
            var outcome = Create().OnTick(Long(1000, 2000, state), 2201);

            Assert.Equal(JudgementType.Miss, outcome.Judgement);
            Assert.Equal(NoteState.Resolved, outcome.NewState);
            Assert.True(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Break, outcome.Combo);
        }

        [Fact]
        public void LongTail_TickDuringHold_DoesNothing()
        {
            AssertNoChange(Create().OnTick(Long(1000, 2000, NoteState.Holding), 1500), NoteState.Holding);
        }

        // ---------------------------------------------------------------
        // Full lifecycles
        // ---------------------------------------------------------------

        [Fact]
        public void Lifecycle_UntouchedLongNote_GivesTwoMisses_HeadThenTail()
        {
            var mechanics = Create();
            var note = Long(1000, 2000);

            var head = Apply(note, mechanics.OnTick(note, 1201));
            Assert.Equal(JudgementType.Miss, head.Judgement);
            Assert.False(head.AdvanceCursor);

            var tail = Apply(note, mechanics.OnTick(note, 2201));
            Assert.Equal(JudgementType.Miss, tail.Judgement);
            Assert.True(tail.AdvanceCursor);
            Assert.Equal(NoteState.Resolved, note.NoteState);
        }

        [Fact]
        public void Lifecycle_MissedHeadThenRepressAndRelease_TailRecovers()
        {
            var mechanics = Create();
            var note = Long(1000, 2000);

            Apply(note, mechanics.OnTick(note, 1201));                              // head Miss -> ReleasedEarly
            Apply(note, mechanics.OnInput(note, InputDirection.Down, 1500));       // repress -> Recovering
            Assert.Equal(NoteState.Recovering, note.NoteState);

            var tail = Apply(note, mechanics.OnInput(note, InputDirection.Up, 2000));
            Assert.Equal(JudgementType.Bad, tail.Judgement);
            Assert.True(tail.AdvanceCursor);
        }

        [Fact]
        public void Lifecycle_DoubleEarlyRelease_ThenCorrectFinalRelease_LocksToBad()
        {
            var mechanics = Create();
            var note = Long(1000, 2000);

            var outcome = Apply(note, mechanics.OnInput(note, InputDirection.Down, 1000));
            Assert.Equal(NoteState.Holding, outcome.NewState);

            outcome = Apply(note, mechanics.OnInput(note, InputDirection.Up, 1500));       // first early release
            Assert.Equal(NoteState.ReleasedEarly, outcome.NewState);
            Assert.Equal(ComboEffect.Break, outcome.Combo);

            outcome = Apply(note, mechanics.OnInput(note, InputDirection.Down, 1650));     // repress
            Assert.Equal(NoteState.Recovering, outcome.NewState);

            outcome = Apply(note, mechanics.OnInput(note, InputDirection.Up, 1700));       // second early release
            Assert.Equal(NoteState.ReleasedEarly, outcome.NewState);
            Assert.Equal(ComboEffect.Break, outcome.Combo);

            outcome = Apply(note, mechanics.OnInput(note, InputDirection.Down, 1750));     // repress again
            Assert.Equal(NoteState.Recovering, outcome.NewState);

            outcome = Apply(note, mechanics.OnInput(note, InputDirection.Up, 2000));       // perfectly timed tail
            Assert.Equal(JudgementType.Bad, outcome.Judgement);                            // still capped at Bad
            Assert.Equal(NoteState.Resolved, outcome.NewState);
            Assert.True(outcome.AdvanceCursor);
        }

        [Fact]
        public void Lifecycle_EarlyRelease_Repress_NeverReleased_ResolvesMissViaTimeout()
        {
            var mechanics = Create();
            var note = Long(1000, 2000);

            Apply(note, mechanics.OnInput(note, InputDirection.Down, 1000));
            Apply(note, mechanics.OnInput(note, InputDirection.Up, 1700));
            Apply(note, mechanics.OnInput(note, InputDirection.Down, 1750));

            var outcome = Apply(note, mechanics.OnTick(note, 2201));

            Assert.Equal(JudgementType.Miss, outcome.Judgement);
            Assert.Equal(NoteState.Resolved, outcome.NewState);
            Assert.True(outcome.AdvanceCursor);
            Assert.Equal(ComboEffect.Break, outcome.Combo);
        }

        [Fact]
        public void Lifecycle_CleanLongNote_GivesTwoIncrementsAndNoBreaks()
        {
            var mechanics = Create();
            var note = Long(1000, 2000);

            var head = Apply(note, mechanics.OnInput(note, InputDirection.Down, 1005));
            var tail = Apply(note, mechanics.OnInput(note, InputDirection.Up, 1995));

            Assert.Equal(ComboEffect.Increment, head.Combo);
            Assert.Equal(ComboEffect.Increment, tail.Combo);
            Assert.True(tail.AdvanceCursor);
        }
    }
}
