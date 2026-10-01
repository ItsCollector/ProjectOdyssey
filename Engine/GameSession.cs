using OpenTK.Mathematics;
using System.Diagnostics;
using ProjectOdyssey.Input;
using ProjectOdyssey.IO;
using System.Runtime.InteropServices.Marshalling;
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
        private float ghostTapThreshold = 200;

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
        private JudgementEngine judgementEngine;

        public GameSession(InputHistory inputHistory, ChartData chartData)
        {
            this.inputHistory = inputHistory;
            NotesByColumn = chartData.NotesByColumn;
            ColumnCursors = new int[NotesByColumn.Length];

            this.judgementEngine = new JudgementEngine(new NativeRuleset());
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

        // Judge one note
        public void JudgeNotes(InputEvent inputEvent, float inputSongTimeMs)
        {
            int column = VkeyToColumn7k(inputEvent.VKey);
            int cursor = ColumnCursors[column];

            if (cursor >= NotesByColumn[column].Length) return; 

            Note note = NotesByColumn[column][cursor];
            InputDirection direction = inputEvent.IsPressed ? InputDirection.Down : InputDirection.Up;

            if (note.NoteType == NoteType.Tap)
            {
                if (direction != InputDirection.Down) return;
                if (Math.Abs(inputSongTimeMs - note.StartTime) > ghostTapThreshold) return;

                (JudgementType judgement, float hitDeviation) = judgementEngine.JudgeHead(inputSongTimeMs, note.StartTime);
                note.NoteState = NoteState.Resolved;
                ColumnCursors[column]++;
                Combo++;
                judgementResultBuffer.Add(new JudgementResult(judgement, hitDeviation, inputSongTimeMs));
                CurrentJudgementResult = new JudgementResult(judgement, hitDeviation, inputSongTimeMs);
                CalculateAccuracyContinuous(hitDeviation);
                return;
            }

            if (note.NoteType == NoteType.Long)
            {
                float hitDeviation; 

                if (note.NoteState == NoteState.Waiting)
                {
                    if (direction != InputDirection.Down) return;
                    if (Math.Abs(inputSongTimeMs - note.StartTime) > ghostTapThreshold) return;

                    (JudgementType headJudgement, hitDeviation) = judgementEngine.JudgeHead(inputSongTimeMs, note.StartTime);
                    note.NoteState = NoteState.Holding;
                    Combo++;
                    judgementResultBuffer.Add(new JudgementResult(headJudgement, hitDeviation, inputSongTimeMs));
                    CurrentJudgementResult = new JudgementResult(headJudgement, hitDeviation, inputSongTimeMs);
                    CalculateAccuracyContinuous(hitDeviation);
                    return;
                }

                (JudgementType tailJudgement, NoteState newState, hitDeviation) = judgementEngine.JudgeTail(inputSongTimeMs, note.EndTime, direction, note.NoteState);

                note.NoteState = newState;

                if (newState == NoteState.ReleasedEarly)
                {
                    Combo = 0; // break combo on early release no matter what
                }

                if (newState == NoteState.Resolved)
                {
                    ColumnCursors[column]++;

                    if (tailJudgement == JudgementType.Miss)
                    {
                        Combo = 0;
                    }
                    else
                    {
                        Combo++;
                    }

                    judgementResultBuffer.Add(new JudgementResult(tailJudgement, hitDeviation, inputSongTimeMs));
                    CurrentJudgementResult = new JudgementResult(tailJudgement, hitDeviation, inputSongTimeMs);
                    CalculateAccuracyContinuous(hitDeviation);
                }

                return;
            }

            Debug.Fail($"[ERROR] Unreachable state because of incorrect NoteType passed: {note.NoteType}");
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

        // This function is specifically for handling notes that the cursor sees but haven't been judged within their windows
        public void HandleUnjudgedNotes(float now)
        {
            for (int i = 0; i < NotesByColumn.Length; i++)
            {
                if (ColumnCursors[i] >= NotesByColumn[i].Length) continue;

                Note note = NotesByColumn[i][ColumnCursors[i]];
                if (note.NoteState == NoteState.Resolved) 
                {
                    ColumnCursors[i]++;
                    continue;
                }

                float timeUntilHit = note.StartTime - now;
                float timeUntilEnd = note.EndTime - now;

                // Missed tap notes that have scrolled past the maximum hit window
                if (note.NoteType == NoteType.Tap && timeUntilHit < -judgementEngine.missWindow)
                {
                    note.NoteState = NoteState.Resolved;
                    Combo = 0;
                    ColumnCursors[i]++;
                    var missResult = new JudgementResult(JudgementType.Miss, 201f, now);
                    judgementResultBuffer.Add(missResult);
                    CurrentJudgementResult = missResult;
                    CalculateAccuracyContinuous(201f);
                    continue;
                }

                // Missed long note heads that have scrolled past the maximum hit window
                if (note.NoteType == NoteType.Long && note.NoteState == NoteState.Waiting && timeUntilHit < -judgementEngine.missWindow)
                {
                    note.NoteState = NoteState.ReleasedEarly;
                    Combo = 0;
                    var missResult = new JudgementResult(JudgementType.Miss, 201f, now);
                    judgementResultBuffer.Add(missResult);
                    CurrentJudgementResult = missResult;
                    CalculateAccuracyContinuous(201f);
                    continue;
                }

                if (note.NoteType == NoteType.Long && (note.NoteState == NoteState.Holding || note.NoteState == NoteState.Recovering || note.NoteState == NoteState.ReleasedEarly))
                {
                    if (judgementEngine.TryResolveOverheldNote(note.NoteState, note.EndTime, now, out var result, out var newState))
                    {
                        note.NoteState = newState;
                        ColumnCursors[i]++;
                        Combo = 0;
                        var missResult = new JudgementResult(JudgementType.Miss, 201f, now);
                        judgementResultBuffer.Add(missResult);
                        CurrentJudgementResult = missResult;
                        CalculateAccuracyContinuous(201f);
                        continue;
                    }
                }
            }
        }

        // Calculate the accuracy of a hit using a continous hit deviation curve
        public void CalculateAccuracyContinuous(float hitDeviation)
        {
            float absHitDeviation = Math.Abs(hitDeviation);
            absHitDeviation = Math.Clamp(absHitDeviation, 0f, ghostTapThreshold);

            float decay = 0.3f;
            float normalised = 1.0f - (absHitDeviation / ghostTapThreshold);
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
