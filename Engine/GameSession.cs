using OpenTK.Mathematics;
using System.Diagnostics;

namespace ProjectOdyssey
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

        private bool notesOverflowPastJudgementLine = true;
        private float ghostTapThreshold = 200;

        public Note[][] notesByColumn { get; set; } // pass these into the function later chart loading is being implemented, and remove nullable
        public int[] columnCursors { get; set; } // construct cursors passed on the number of columns in the chart, and remove nullable

        public GameSession(InputHistory inputHistory, ChartData chartData)
        {
            this.inputHistory = inputHistory;
            this.notesByColumn = chartData.notesByColumn;
            columnCursors = new int[this.notesByColumn.Length];
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
                    float now = (float)gameClock.CurrentSongTimeMs;

                    while (inputHistory.TryGetNextEvent(out InputEvent inputEvent))
                    {
                        float inputSongTimeMs = (float)gameClock.ToSongTimeMs(inputEvent.TimeStamp);
                        JudgeNotes(inputEvent, inputSongTimeMs);
                    }

                    HandleUnjudgedNotes(now);
                    UpdateNotePositions(now);

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
            int cursor = columnCursors[column];

            if (cursor >= notesByColumn[column].Length) return;

            Note note = notesByColumn[column][cursor];
            InputDirection direction = inputEvent.IsPressed ? InputDirection.Down : InputDirection.Up;
 
            if (note.noteType == NoteType.Tap)
            {
                if (direction != InputDirection.Down) return;
                if (Math.Abs(inputSongTimeMs - note.startTime) > ghostTapThreshold) return;

                JudgementType judgement = JudgementEngine.JudgeHead(inputSongTimeMs, note.startTime);
                note.noteState = NoteState.Resolved;
                columnCursors[column]++;

                Console.WriteLine($"[JUDGEMENT] Vkey: {column + 1} Position: Tap Note | Note ST: {note.startTime} Note ET: {note.endTime} | Direction: {direction} | Judge: {judgement}");
                return;
            }

            if (note.noteType == NoteType.Long)
            {
                if (note.noteState == NoteState.Waiting)
                {
                    if (direction != InputDirection.Down) return;
                    if (Math.Abs(inputSongTimeMs - note.startTime) > ghostTapThreshold) return;

                    JudgementType headJudgement = JudgementEngine.JudgeHead(inputSongTimeMs, note.startTime);
                    note.noteState = NoteState.Holding;

                    Console.WriteLine($"[JUDGEMENT] Vkey: {column + 1} Position: Long Note | Note ST: {note.startTime} Note ET: {note.endTime} | Direction: {direction} | Judge: {headJudgement}");
                    return;
                }

                (JudgementType tailJudgement, NoteState newState) =
                    JudgementEngine.JudgeTail(inputSongTimeMs, note.endTime, direction, note.noteState);

                note.noteState = newState;

                if (newState == NoteState.Resolved)
                {
                    columnCursors[column]++;
                }

                Console.WriteLine($"[JUDGEMENT] Vkey: {column + 1} Position: Long Note | Note ST: {note.startTime} Note ET: {note.endTime} | Direction: {direction} | Judge: {tailJudgement}");
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
            for (int i = 0; i < notesByColumn.Length; i++) // Iterate through each column
            {
                for (int j = 0; j < (notesByColumn[i].Length - columnCursors[i]); j++) // Iterate through each note in the column
                {
                    Note note = notesByColumn[i][j + columnCursors[i]];

                    float timeUntilHit = note.startTime - now;
                    float timeUntilEnd = note.endTime - now;

                    if (note.noteType == NoteType.Tap)
                    {
                        float tHead = 1f - (timeUntilHit / approachTime);
                        tHead = notesOverflowPastJudgementLine ? tHead : Math.Min(tHead, 1f);
                        note.headPosY = MathHelper.Lerp(spawnPositionY, hitPositionY, tHead);
                    }
                    if (note.noteType == NoteType.Long)
                    {
                        float tHead = 1f - (timeUntilHit / approachTime);
                        float tTail = 1f - (timeUntilEnd / approachTime);

                        tHead = notesOverflowPastJudgementLine ? tHead : Math.Min(tHead, 1f);
                        tTail = notesOverflowPastJudgementLine ? tTail : Math.Min(tTail, 1f);

                        note.headPosY = MathHelper.Lerp(spawnPositionY, hitPositionY, tHead);
                        note.tailPosY = MathHelper.Lerp(spawnPositionY, hitPositionY, tTail);
                    }
                }
            }
        }

        // This function is specifically for handling notes that the cursor sees but haven't been judged within their windows
        public void HandleUnjudgedNotes(float now)
        {
            for (int i = 0; i < notesByColumn.Length; i++)
            {
                if (columnCursors[i] >= notesByColumn[i].Length) continue;

                Note note = notesByColumn[i][columnCursors[i]];

                if (note.noteState == NoteState.Resolved) // maybe move into judgement block
                {
                    columnCursors[i]++;
                    continue;
                }

                float timeUntilHit = note.startTime - now;
                float timeUntilEnd = note.endTime - now;

                if (note.noteType == NoteType.Tap && timeUntilHit < -JudgementEngine.missWindowMs)
                {
                    Console.WriteLine($"[JUDGEMENT] Vkey: {note.column + 1} Position: Tap Note | Note ST: {note.startTime} Note ET: {note.endTime} | Judge: Miss");
                    note.noteState = NoteState.Resolved;
                    columnCursors[i]++;
                    continue;
                }

                if (note.noteType == NoteType.Long && note.noteState == NoteState.Waiting && timeUntilHit < -JudgementEngine.missWindowMs)
                {
                    // TODO: record as a Miss
                    Console.WriteLine($"[JUDGEMENT] Vkey: {note.column + 1} Position: Long Note | Note ST: {note.startTime} Note ET: {note.endTime} | Judge: Miss");
                    note.noteState = NoteState.ReleasedEarly;
                    continue;
                }

                if (note.noteType == NoteType.Long && (note.noteState == NoteState.Holding || note.noteState == NoteState.Recovering || note.noteState == NoteState.ReleasedEarly))
                {
                    if (JudgementEngine.TryResolveOverheldNote(note.noteState, note.endTime, now, out var result, out var newState))
                    {
                        note.noteState = newState;
                        columnCursors[i]++;

                        // TODO: record `result` as a Miss
                        Console.WriteLine($"[JUDGEMENT] Vkey: {note.column + 1} Position: Long Note | Note ST: {note.startTime} Note ET: {note.endTime} | Judge: Miss");
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
