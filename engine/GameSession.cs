using System.Diagnostics;
using System.Threading;

namespace ProjectOdyssey
{
    public class GameSession
    {
        private Thread? gameplayThread;
        private GameClock gameClock = new();
        private InputHistory inputHistory;
        private volatile bool isRunning;

        private float approachTime = 420;
        private float spawnPositionY = -30;
        private float hitPositionY = 1000;

        private Note[][]? notesByColumn;
        private int[]? columnCursors;

        public GameSession(InputHistory inputHistory)
        {
            this.inputHistory = inputHistory;
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
                    if (inputHistory.TryGetNextEvent(out InputEvent inputEvent))
                    {
                        Console.WriteLine($"[Input] VKey={inputEvent.VKey}, IsPressed={inputEvent.IsPressed}, TimeStamp={inputEvent.TimeStamp}");
                    }

                    lastTime = currentTime;
                }
                else
                {
                    Thread.Yield();
                }
            }
        }

        public void JudgeNotes()
        {

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
                }
            }
        }
    }
}
