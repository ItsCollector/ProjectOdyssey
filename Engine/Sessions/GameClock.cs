using System.Diagnostics;

namespace ProjectOdyssey.Engine
{
    public class GameClock
    {
        private long startTimestamp;
        private double globalOffsetMs;
        private double leadInMs = 2000;
        private static readonly double TicksPerMs = Stopwatch.Frequency / 1000.0;

        private volatile bool isPaused;
        private long pauseStartTimestamp;
        private long accumulatedPausedTicks;

        public bool IsPaused => isPaused;

        public void Start(double globalOffsetMs)
        {
            this.globalOffsetMs = globalOffsetMs;
            startTimestamp = Stopwatch.GetTimestamp();
            accumulatedPausedTicks = 0;
            isPaused = false;
        }

        public void Pause()
        {
            if (isPaused) return;
            pauseStartTimestamp = Stopwatch.GetTimestamp();
            isPaused = true;
        }

        public void Resume()
        {
            if (!isPaused) return;
            accumulatedPausedTicks += Stopwatch.GetTimestamp() - pauseStartTimestamp;
            isPaused = false;
        }

        public double ToSongTimeMs(long rawTimestamp)
        {
            // While paused, freeze at the exact moment pause began, regardless
            // of what raw timestamp is passed in - stops notes/judging from
            // advancing using a "now" that occurred after the pause started.
            long effectiveTimestamp = isPaused ? pauseStartTimestamp : rawTimestamp;
            return (effectiveTimestamp - startTimestamp - accumulatedPausedTicks) / TicksPerMs - leadInMs - globalOffsetMs;
        }

        public double CurrentSongTimeMs => ToSongTimeMs(Stopwatch.GetTimestamp());
    }
}