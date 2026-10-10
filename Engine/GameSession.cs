using OpenTK.Mathematics;
using System.Diagnostics;
using ProjectOdyssey.Input;
using ProjectOdyssey.IO;
using System.Runtime.InteropServices.Marshalling;
using ProjectOdyssey.Engine.Mechanics;
using ProjectOdyssey.Engine.Rulesets;

namespace ProjectOdyssey.Engine
{
    public class GameSession
    {
        // Collaborators
        private readonly InputHistory inputHistory;
        private readonly RulesetMechanics mechanics;
        private readonly GameClock gameClock = new();

        // Gameplay thread
        private Thread? gameplayThread;
        private volatile bool isRunning;
        private volatile bool audioReadyToStart = false;

        // Input mapping
        // VK code -> column index, or -1 if that key isn't bound in this chart's layout.
        // Built once in the constructor and read-only afterwards, so the gameplay thread can read it without locks.
        private readonly int[] columnByVKey = new int[256];

        // Note scrolling (arbitrary values that should be moved to a config / skinning later)
        private float approachTime = 420;
        private float spawnPositionY = -100;
        private float hitPositionY = 1000;
        private bool notesOverflowPastJudgementLine = false;

        // Judgement history
        private readonly JudgementResultBuffer judgementResultBuffer = new(300);
        private readonly JudgementResult[] recentScratch = new JudgementResult[300];

        // Accuracy tracking
        private int judgedNotesCount = 0;
        private float accuracyAccumulator = 0f;

        // Public state read by the screen / views
        public bool AudioReadyToStart => audioReadyToStart;
        public float CurrentSongTimeMs => (float)gameClock.CurrentSongTimeMs;
        public Note[][] NotesByColumn { get; set; } // pass these into the function later chart loading is being implemented, and remove nullable
        public int[] ColumnCursors { get; set; } // construct cursors passed on the number of columns in the chart, and remove nullable
        public int Combo { get; private set; } = 0;
        public float Accuracy { get; private set; } = 100f;
        public JudgementResult CurrentJudgementResult { get; private set; } = new JudgementResult(JudgementType.Marvellous, 0f, float.NegativeInfinity);

        public GameSession(InputHistory inputHistory, ChartData chartData, IRuleset? ruleset = null)
        {
            this.inputHistory = inputHistory;
            NotesByColumn = chartData.NotesByColumn;
            ColumnCursors = new int[NotesByColumn.Length];

            ruleset ??= new NativeRuleset();
            mechanics = ruleset.CreateMechanics();

            var result = VirtualKeyMapper.GetManiaBindings(chartData.KeyCount);

            if (!result.IsSuccess)
            {
                throw new Exception(result.Error);

                // handle error gracefully later
            }

            ushort[] binds = result.Value;

            // The bind list and the chart's columns must line up, otherwise a bound key could index past NotesByColumn
            if (binds.Length != NotesByColumn.Length)
            {
                throw new InvalidOperationException($"Chart has {NotesByColumn.Length} columns but {binds.Length} key binds were found for {chartData.KeyCount}K.");
            }

            Array.Fill(columnByVKey, -1);

            for (int column = 0; column < binds.Length; column++)
            {
                ushort vKey = binds[column];

                if (vKey < columnByVKey.Length)
                {
                    columnByVKey[vKey] = column;
                }
            }
        }

        public void Start(ChartData chartData)
        {
            isRunning = true;
            gameplayThread = new Thread(Run);
            gameplayThread.IsBackground = true;
            gameplayThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            gameplayThread?.Join();
        }

        public void Pause()
        {
            gameClock.Pause();
            while (inputHistory.TryGetNextEvent(out _)) { } // drop input queued during the pause
        }

        public void Resume()
        {
            while (inputHistory.TryGetNextEvent(out _)) { } // drop input queued during the pause
            gameClock.Resume();
        }

        public void Run()
        {
            gameClock.Start(globalOffsetMs: 0);
            while (inputHistory.TryGetNextEvent(out _)) { } // drop any input queued before the session started

            var stopwatch = Stopwatch.StartNew();
            double lastTime = stopwatch.Elapsed.TotalSeconds;
            const double targetDelta = 0.001; // 1000Hz tick rate

            while (isRunning)
            {
                double currentTime = stopwatch.Elapsed.TotalSeconds;

                if (currentTime - lastTime >= targetDelta)
                {
                    if (!gameClock.IsPaused)
                    {
                        float now = (float)gameClock.CurrentSongTimeMs;

                        if (!audioReadyToStart && now >= 0f)
                        {
                            audioReadyToStart = true;
                        }

                        while (inputHistory.TryGetNextEvent(out InputEvent inputEvent))
                        {
                            float inputSongTimeMs = (float)gameClock.ToSongTimeMs(inputEvent.TimeStamp);
                            JudgeNotes(inputEvent, inputSongTimeMs);
                        }

                        HandleUnjudgedNotes(now);
                        UpdateNotePositions(now);
                    }

                    lastTime = currentTime;
                }
                else
                {
                    Thread.Yield();
                }
            }
        }

        // Hand an input event to the mechanics for the note at the front of its column and apply the result
        public void JudgeNotes(InputEvent inputEvent, float inputSongTimeMs)
        {
            int column = inputEvent.VKey < columnByVKey.Length ? columnByVKey[inputEvent.VKey] : -1;
            if (column < 0) return; // key isn't bound in this layout

            int cursor = ColumnCursors[column];

            if (cursor >= NotesByColumn[column].Length) return;

            Note note = NotesByColumn[column][cursor];
            InputDirection direction = inputEvent.IsPressed ? InputDirection.Down : InputDirection.Up;

            ApplyOutcome(mechanics.OnInput(note, direction, inputSongTimeMs), note, column, inputSongTimeMs);
        }

        public void UpdateNotePositions(float now)
        {
            for (int i = 0; i < NotesByColumn.Length; i++) // Iterate through each column
            {
                for (int j = 0; j < (NotesByColumn[i].Length - ColumnCursors[i]); j++) // Iterate through each note in the column
                {
                    Note note = NotesByColumn[i][j + ColumnCursors[i]];
                    float timeUntilHit = note.StartTime - now;
                    float timeUntilEnd = note.EndTime - now;

                    if (note.NoteType == NoteType.Tap)
                    {
                        float tHead = 1f - (timeUntilHit / approachTime);
                        tHead = notesOverflowPastJudgementLine ? tHead : Math.Min(tHead, 1f);
                        note.HeadPosY = MathHelper.Lerp(spawnPositionY, hitPositionY, tHead);
                    }
                    if (note.NoteType == NoteType.Long)
                    {
                        float tHead = 1f - (timeUntilHit / approachTime);
                        float tTail = 1f - (timeUntilEnd / approachTime);

                        tHead = notesOverflowPastJudgementLine ? tHead : Math.Min(tHead, 1f);
                        tTail = notesOverflowPastJudgementLine ? tTail : Math.Min(tTail, 1f);

                        note.HeadPosY = MathHelper.Lerp(spawnPositionY, hitPositionY, tHead);
                        note.TailPosY = MathHelper.Lerp(spawnPositionY, hitPositionY, tTail);
                    }
                }
            }
        }

        // Let the mechanics resolve notes that time has passed on without a (further) input:
        // taps/heads that scrolled past the miss window, long notes held past their tail, etc.
        public void HandleUnjudgedNotes(float now)
        {
            for (int i = 0; i < NotesByColumn.Length; i++)
            {
                if (ColumnCursors[i] >= NotesByColumn[i].Length) continue;

                Note note = NotesByColumn[i][ColumnCursors[i]];
                ApplyOutcome(mechanics.OnTick(note, now), note, i, now);
            }
        }

        // Apply exactly what the mechanics decided. No interpretation of why happens here.
        private void ApplyOutcome(in MechanicsOutcome outcome, Note note, int column, float judgedAtMs)
        {
            note.NoteState = outcome.NewState;

            if (outcome.AdvanceCursor)
            {
                ColumnCursors[column]++;
            }

            switch (outcome.Combo)
            {
                case ComboEffect.Increment:
                    Combo++;
                    break;
                case ComboEffect.Break:
                    Combo = 0;
                    break;
            }

            if (outcome.Judgement is JudgementType judgement)
            {
                var result = new JudgementResult(judgement, outcome.HitDeviation, judgedAtMs);
                judgementResultBuffer.Add(result);
                CurrentJudgementResult = result;
                CalculateAccuracyContinuous(outcome.HitDeviation);
            }
        }

        // Calculate the accuracy of a hit using a continous hit deviation curve
        public void CalculateAccuracyContinuous(float hitDeviation)
        {
            float absHitDeviation = Math.Abs(hitDeviation);
            absHitDeviation = Math.Clamp(absHitDeviation, 0f, mechanics.GhostTapThreshold);

            float decay = 0.3f;
            float normalised = 1.0f - (absHitDeviation / mechanics.GhostTapThreshold);
            float accuracyContribution = (float)Math.Pow(normalised, decay);

            accuracyAccumulator += (float)Math.Round(accuracyContribution, 3);
            judgedNotesCount++;
            CalculateFinalAccuracy();
        }

        public void CalculateFinalAccuracy()
        {
            if (judgedNotesCount == 0)
            {
                Accuracy = 100f;
            }
            else
            {
                Accuracy = (accuracyAccumulator / judgedNotesCount) * 100f;
            }
        }

        public ReadOnlySpan<JudgementResult> GetRecentJudgementResults()
        {
            int n = judgementResultBuffer.CopyTo(recentScratch);
            return recentScratch.AsSpan(0, n);
        }
    }
}