using System.Diagnostics;

namespace ProjectOdyssey
{
    public class GameClock
    {
        private long startTimestamp;
        private double globalOffsetMs;
        private double leadInMs = 2000; // grace period before song-time 0, so players can get fingers on keys for early notes
        private static readonly double TicksPerMs = Stopwatch.Frequency / 1000.0;

        public void Start(double globalOffsetMs)
        {
            this.globalOffsetMs = globalOffsetMs;
            startTimestamp = Stopwatch.GetTimestamp();
        }

        public double ToSongTimeMs(long rawTimestamp)
            => (rawTimestamp - startTimestamp) / TicksPerMs - leadInMs - globalOffsetMs;

        public double CurrentSongTimeMs => ToSongTimeMs(Stopwatch.GetTimestamp());
    }
}