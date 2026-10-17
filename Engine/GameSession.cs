using ProjectOdyssey.Engine.Mechanics;
using ProjectOdyssey.Engine.Rulesets;
using ProjectOdyssey.Engine.Scoring;
using ProjectOdyssey.Input;
using ProjectOdyssey.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
        public float ApproachTime = 450;
        public float SpawnPositionY = 0;
        private float hitPositionY = 1000;
        private bool notesOverflowPastJudgementLine = false;

        public JudgementResult CurrentJudgementResult { get; private set; } = new JudgementResult(JudgementType.Marvellous, 0f, float.NegativeInfinity);
        public int TotalNotes;
        public int ResolvedNotes;

        // Judgement history
        private readonly JudgementResultBuffer judgementResultBuffer = new(300);
        private readonly JudgementResult[] recentScratch = new JudgementResult[300];
        public List<ReplayEvent> recordedInputs;

        // Accuracy tracking
        public ScoreManager Score;
        public int Combo => Score.CurrentCombo;
        public float Accuracy => Score.CurrentContinuousAccuracy;

        // Public state read by the screen / views
        public bool AudioReadyToStart => audioReadyToStart;
        public float CurrentSongTimeMs => (float)gameClock.CurrentSongTimeMs;
        private ChartData chartData; // Default, copy of notes by column is used to ensure this isn't changed

        // Active copy of chart's note being worked on and active copy of cursors for chart's columns being used
        // Reset these to default state with InitialiseSession() if you restart the chart 
        public Note[][] NotesByColumn { get; set; } 
        public int[] ColumnCursors { get; set; }

        public GameSession(InputHistory inputHistory, ChartData incomingChartData, float hitPositionY, IRuleset? ruleset = null)
        {
            chartData = incomingChartData;
            this.inputHistory = inputHistory;
            this.hitPositionY = hitPositionY;

            ruleset ??= new NativeRuleset();
            mechanics = ruleset.CreateMechanics();

            InitialiseSession();
            LoadManiaBindings();
        }

        [MemberNotNull(nameof(NotesByColumn), nameof(ColumnCursors), nameof(recordedInputs), nameof(Score))]
        public void InitialiseSession()
        {
            // Fresh copy of the notes with default state
            NotesByColumn = chartData.NotesByColumn
                .Select(column => column.Select(note => note.Clone()).ToArray())
                .ToArray();


            // Initialise cursors
            ColumnCursors = new int[NotesByColumn.Length];

            TotalNotes = 0;

            // Calculate total notes
            foreach (var column in NotesByColumn)
            {
                this.TotalNotes += column.Length;
            }

            recordedInputs = new List<ReplayEvent>(TotalNotes * 2 + 1024);

            // Scoring
            ResolvedNotes = 0;
            Score = new ScoreManager(mechanics);

            // Judgement buffer for HUD
            judgementResultBuffer.Clear();
            CurrentJudgementResult = new JudgementResult(JudgementType.Marvellous, 0f, float.NegativeInfinity);

            // Mark audio not ready
            audioReadyToStart = false;
        }

        public void LoadManiaBindings()
        {
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

        public void Start()
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
            Score.IncrementPauseCount();
            while (inputHistory.TryGetNextEvent(out _)) { } // drop input queued during the pause
        }

        public void Resume()
        {
            while (inputHistory.TryGetNextEvent(out _)) { } // drop input queued during the pause
            gameClock.Resume();
        }

        public void Restart()
        {
            Stop();
            InitialiseSession();
            Start();
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
                            recordedInputs.Add(new ReplayEvent(inputSongTimeMs, inputEvent.VKey, inputEvent.IsPressed));
                            JudgeNotes(inputEvent, inputSongTimeMs);
                        }

                        HandleUnjudgedNotes(now);
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
                ResolvedNotes++;
            }

            Score.UpdateCombo(outcome.Combo);

            if (outcome.Judgement is JudgementType judgement)
            {
                var result = new JudgementResult(judgement, outcome.HitDeviation, judgedAtMs);
                judgementResultBuffer.Add(result);
                CurrentJudgementResult = result;

                Score.AccumulateJudgement(result.Type);
                Score.CalculateAccuracyContinuous(result.Type, outcome.HitDeviation);
                Score.CalculateAccuracyDiscrete(result.Type);
            }
        }

        public ReadOnlySpan<JudgementResult> GetRecentJudgementResults()
        {
            int n = judgementResultBuffer.CopyTo(recentScratch);
            return recentScratch.AsSpan(0, n);
        }
    }
}