using OpenTK.Mathematics;
using System.Diagnostics;
using ProjectOdyssey.Input;
using ProjectOdyssey.IO;

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

        public Note[][] NotesByColumn { get; set; } // pass these into the function later chart loading is being implemented, and remove nullable
        public int[] ColumnCursors { get; set; } // construct cursors passed on the number of columns in the chart, and remove nullable

        public GameSession(InputHistory inputHistory, ChartData chartData)
        {
            this.inputHistory = inputHistory;
            NotesByColumn = chartData.NotesByColumn;
            ColumnCursors = new int[NotesByColumn.Length];
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

                JudgementType judgement = JudgementEngine.JudgeHead(inputSongTimeMs, note.StartTime);
                note.NoteState = NoteState.Resolved;
                ColumnCursors[column]++;

                //Console.WriteLine($"[JUDGEMENT] Vkey: {column + 1} Position: Tap Note | Note ST: {note.StartTime} Note ET: {note.EndTime} | Direction: {direction} | Judge: {judgement}");
                return;
            }

            if (note.NoteType == NoteType.Long)
            {
                if (note.NoteState == NoteState.Waiting)
                {
                    if (direction != InputDirection.Down) return;
                    if (Math.Abs(inputSongTimeMs - note.StartTime) > ghostTapThreshold) return;

                    JudgementType headJudgement = JudgementEngine.JudgeHead(inputSongTimeMs, note.StartTime);
                    note.NoteState = NoteState.Holding;

                    //Console.WriteLine($"[JUDGEMENT] Vkey: {column + 1} Position: Long Note | Note ST: {note.StartTime} Note ET: {note.EndTime} | Direction: {direction} | Judge: {headJudgement}");
                    return;
                }

                (JudgementType tailJudgement, NoteState newState) =
                    JudgementEngine.JudgeTail(inputSongTimeMs, note.EndTime, direction, note.NoteState);

                note.NoteState = newState;

                if (newState == NoteState.Resolved)
                {
                    ColumnCursors[column]++;
                }

                //Console.WriteLine($"[JUDGEMENT] Vkey: {column + 1} Position: Long Note | Note ST: {note.startTime} Note ET: {note.endTime} | Direction: {direction} | Judge: {tailJudgement}");
            }
        }

        /*  Some key documentation so I don't get confused when trying to map out the logic. 
         * 
         *  Tap notes that are marked as "Resolved" should never find their way into this function because
         *  upon judgement, the cursor for the corresponding key column will increment, moving this resolved note
         *  out of scope for when the UpdateNotePositions() function is called. 
         * 
         *  This mean that all notes that are read in this function are always unresolved in some form. 
         *  
         *  Cases include: 
         *  - Tap notes that are still in the approach phase (not yet hit).
         *  - All long notes until the tail's hitbox has completely passed the judgement line
         *    regardless of whatever happened to the head note. 
         *    
         *  The NoteState.Waiting should probaby be changed to NoteState.Approaching to better reflect the state of the note.
         *  
         *  The only responsibility of this function should be to move unresolved notes along the Y axis based on the current
         *  time and the note's start and end times in accordance to the cases mentioned. 
         */
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

                if (note.NoteType == NoteType.Tap && timeUntilHit < -JudgementEngine.MissWindowMs)
                {
                    //Console.WriteLine($"[JUDGEMENT] Vkey: {note.Column + 1} Position: Tap Note | Note ST: {note.StartTime} Note ET: {note.EndTime} | Judge: Miss");
                    note.NoteState = NoteState.Resolved;
                    ColumnCursors[i]++;
                    continue;
                }

                if (note.NoteType == NoteType.Long && note.NoteState == NoteState.Waiting && timeUntilHit < -JudgementEngine.MissWindowMs)
                {
                    // TODO: record as a Miss
                    //Console.WriteLine($"[JUDGEMENT] Vkey: {note.Column + 1} Position: Long Note | Note ST: {note.StartTime} Note ET: {note.EndTime} | Judge: Miss");
                    note.NoteState = NoteState.ReleasedEarly;
                    continue;
                }

                if (note.NoteType == NoteType.Long && (note.NoteState == NoteState.Holding || note.NoteState == NoteState.Recovering || note.NoteState == NoteState.ReleasedEarly))
                {
                    if (JudgementEngine.TryResolveOverheldNote(note.NoteState, note.EndTime, now, out var result, out var newState))
                    {
                        note.NoteState = newState;
                        ColumnCursors[i]++;

                        // TODO: record `result` as a Miss
                        //Console.WriteLine($"[JUDGEMENT] Vkey: {note.Column + 1} Position: Long Note | Note ST: {note.StartTime} Note ET: {note.EndTime} | Judge: Miss");
                        continue;
                    }
                }
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
