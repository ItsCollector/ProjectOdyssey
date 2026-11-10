using System.Diagnostics;

namespace ProjectOdyssey.App
{
    public sealed class FrameLimiter
    {
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private long next;

        public int TargetFps { get; set; }   // 0 or less = no limit

        // Call once per frame, right after SwapBuffers
        public void Wait()
        {
            long now = clock.ElapsedTicks;
            if (TargetFps <= 0) { next = now; return; }

            long frame = Stopwatch.Frequency / TargetFps;
            next += frame;
            if (next < now - frame) next = now;   // fell behind: don't fire a burst of frames to catch up

            while (true)
            {
                long remaining = next - clock.ElapsedTicks;
                if (remaining <= 0) break;

                if (remaining * 1000 / Stopwatch.Frequency > 4) Thread.Sleep(1);   // far away: give the CPU back
                else Thread.SpinWait(50);                                          // close: spin for precision
            }
        }
    }
}   
