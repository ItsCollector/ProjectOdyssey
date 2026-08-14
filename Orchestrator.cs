using System.Diagnostics;
using System.Threading;

namespace ProjectOdyssey
{
    public class Orchestrator
    {
        private Thread? gameplayThread;
        private GameClock gameClock = new();
        private InputHistory inputHistory;
        private volatile bool isRunning;

        public Orchestrator(InputHistory inputHistory)
        {
            this.inputHistory = inputHistory;
        }

        public void Start()
        {
            isRunning = true;
            gameplayThread = new Thread(GameplayLoop);
            gameplayThread.IsBackground = true;
            gameplayThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            gameplayThread?.Join(); 
        }

        public void GameplayLoop()
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
    }
}
