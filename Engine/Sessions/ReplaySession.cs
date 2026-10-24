using ProjectOdyssey.Engine.Mechanics;
using ProjectOdyssey.Engine.Rulesets;
using ProjectOdyssey.Engine.Scoring;
using ProjectOdyssey.Input;
using ProjectOdyssey.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ProjectOdyssey.Engine
{
    public class ReplaySession
    {
        private readonly RulesetMechanics mechanics;
        private readonly GameClock gameClock = new();

        private Thread? replayThread;
        private volatile bool isRunning;
        private volatile bool audioReadyToStart = false;

        private readonly int[] columnByVKey;

        public float ApproachTime = 420;
        public float SpawnPositionY = 0;

        public JudgementResult CurrentJudgementResult { get; private set; } = new JudgementResult(JudgementType.Marvellous, 0f, float.NegativeInfinity);
        public int TotalNotes;
        public int ResolvedNotes;

        private readonly JudgementResultBuffer judgementResultBuffer = new(300);
        private readonly JudgementResult[] recentScratch = new JudgementResult[300];

        public ScoreManager Score;
        public int Combo => Score.CurrentCombo;
        public float Accuracy => Score.CurrentContinuousAccuracy;

        public bool AudioReadyToStart => audioReadyToStart;
        public float CurrentSongTimeMs => (float)gameClock.CurrentSongTimeMs;
        private ChartData chartData;
        private ReplayData replayData;

        public Note[][] NotesByColumn { get; set; }
        public int[] ColumnCursors { get; set; }

        private int replayCursor;
        private ReplayEvent[] events => replayData.ReplayEvents;   // whatever ReplayData exposes

        public ReplaySession(ReplayData replayData, ChartData incomingChartData, IRuleset? ruleset = null)
        {
            this.replayData = replayData;
            chartData = incomingChartData;

            ruleset ??= new NativeRuleset();
            mechanics = ruleset.CreateMechanics();

            InitialiseSession();
            columnByVKey = replayData.ColumnByVKey;
        }

        [MemberNotNull(nameof(NotesByColumn), nameof(ColumnCursors), nameof(Score))]
        public void InitialiseSession()
        {
            replayCursor = 0;

            NotesByColumn = chartData.NotesByColumn
                .Select(column => column.Select(note => note.Clone()).ToArray())
                .ToArray();

            ColumnCursors = new int[NotesByColumn.Length];

            TotalNotes = 0;

            foreach (var column in NotesByColumn)
            {
                this.TotalNotes += column.Length;
            }

            ResolvedNotes = 0;
            Score = new ScoreManager(mechanics);

            judgementResultBuffer.Clear();
            CurrentJudgementResult = new JudgementResult(JudgementType.Marvellous, 0f, float.NegativeInfinity);

            audioReadyToStart = false;
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

                        while (replayCursor < events.Length && events[replayCursor].SongTimeMs <= now)
                        {
                            var e = events[replayCursor++];
                            HandleUnjudgedNotes(e.SongTimeMs);
                            JudgeNotes(e.VKey, e.IsPressed, e.SongTimeMs);
                        }

                        HandleUnjudgedNotes(now);

                        if (!audioReadyToStart && now >= 0f)
                        {
                            audioReadyToStart = true;
                        }
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
        public void JudgeNotes(ushort vKey, bool isPressed, float inputSongTimeMs)
        {
            int column = vKey < columnByVKey.Length ? columnByVKey[vKey] : -1;
            if (column < 0) return;

            int cursor = ColumnCursors[column];
            if (cursor >= NotesByColumn[column].Length) return;

            Note note = NotesByColumn[column][cursor];
            InputDirection direction = isPressed ? InputDirection.Down : InputDirection.Up;

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

        public void Start()
        {
            isRunning = true;
            replayThread = new Thread(Run);
            replayThread.IsBackground = true;
            replayThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            replayThread?.Join();
        }

        public void Pause()
        {
            gameClock.Pause();
        }

        public void Resume()
        {
            gameClock.Resume();
        }

        public void Restart()
        {
            Stop();
            InitialiseSession();
            Start();
        }
    }
}
