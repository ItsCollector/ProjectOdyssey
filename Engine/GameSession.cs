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
        private Thread? gameplayThread;
        private GameClock gameClock = new();
        private InputHistory inputHistory;
        private volatile bool isRunning;

        private float approachTime = 420; // arbitrary values that should be moved to a config / skinning later
        private float spawnPositionY = -100;
        private float hitPositionY = 1000;

        private bool notesOverflowPastJudgementLine = false;

        private volatile bool audioReadyToStart = false;
        public bool AudioReadyToStart => audioReadyToStart;
        public float CurrentSongTimeMs => (float)gameClock.CurrentSongTimeMs;
        public Note[][] NotesByColumn { get; set; } // pass these into the function later chart loading is being implemented, and remove nullable
        public int[] ColumnCursors { get; set; } // construct cursors passed on the number of columns in the chart, and remove nullable
        public int Combo { get; private set; } = 0;
        public JudgementResult[] GetRecentJudgementResults() => judgementResultBuffer.Snapshot();
        public JudgementResult CurrentJudgementResult { get; private set; } = new JudgementResult(JudgementType.Marvellous, 0f, float.NegativeInfinity);

        private JudgementResultBuffer judgementResultBuffer = new(300);
        private int judgedNotesCount = 0;
        private float accuracyAccumulator = 0f;
        public float Accuracy { get; private set; } = 100f;
        private readonly RulesetMechanics mechanics;

        public GameSession(InputHistory inputHistory, ChartData chartData, IRuleset? ruleset = null)
        {
            this.inputHistory = inputHistory;
            NotesByColumn = chartData.NotesByColumn;
            ColumnCursors = new int[NotesByColumn.Length];

            ruleset ??= new NativeRuleset();
            mechanics = ruleset.CreateMechanics();
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
            gameClock.Resume();
        }

        public void Run()
        {
            gameClock.Start(globalOffsetMs: 0);
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
            int column = VkeyToColumn7k(inputEvent.VKey);
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

        private int VkeyToColumn7k(ushort key)
        {
            return key switch
            {
                83 => 0, // S
                68 => 1, // D
                70 => 2, // F
                32 => 3, // Space
                74 => 4, // J
                75 => 5, // K
                76 => 6, // L
                _ => throw new ArgumentException($"Invalid key code: {key}")
            };
        }
    }
}
