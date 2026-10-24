using System.Diagnostics;
using System.Globalization;

namespace ProjectOdyssey.Diagnostic
{
    // Records one sample per rendered frame and writes them to a CSV on request.
    // MainWindow only has to call RecordUpdate / CountInputEvent / RecordFrame and, at shutdown, WriteCsv.
    // When disabled, every method returns immediately and no sample memory is allocated.
    public sealed class FrameProfiler
    {
        private const int MaxFrames = 200_000;
        private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;

        private struct FrameSample
        {
            public double FrameMs;       // time since the previous frame, as reported by the window loop
            public long AllocatedBytes;  // total bytes allocated by all threads so far
            public int Gen2Count;        // gen2 collections so far
            public string Screen;        // which screen was on top this frame
            public double UpdateMs;      // time in OnUpdateFrame since the previous render
            public double RenderMs;      // our own render work, before SwapBuffers
            public double SwapMs;        // time inside SwapBuffers (driver / vsync / compositor wait)
            public int InputEvents;      // raw key events received since the previous render
        }

        private readonly FrameSample[] samples;
        private int count;
        private double updateMsSinceLastRender;
        private int inputEventsSinceLastRender;

        public bool IsEnabled { get; }

        public FrameProfiler(bool isEnabled = true)
        {
            IsEnabled = isEnabled;
            samples = isEnabled ? new FrameSample[MaxFrames] : Array.Empty<FrameSample>();
        }

        // High-resolution timestamp to pass back into RecordUpdate / RecordFrame
        public static long Timestamp() => Stopwatch.GetTimestamp();

        // Call at the end of OnUpdateFrame with the timestamp taken at its start
        public void RecordUpdate(long startTimestamp)
        {
            if (!IsEnabled) return;

            updateMsSinceLastRender += (Stopwatch.GetTimestamp() - startTimestamp) * TicksToMs;
        }

        // Call for every raw key event (safe to call from any thread)
        public void CountInputEvent()
        {
            if (!IsEnabled) return;

            Interlocked.Increment(ref inputEventsSinceLastRender);
        }

        // Call once per frame, right after SwapBuffers
        public void RecordFrame(double frameMs, object? currentScreen, long renderStart, long beforeSwap, long afterSwap)
        {
            if (!IsEnabled) return;

            if (count < MaxFrames)
            {
                samples[count++] = new FrameSample
                {
                    FrameMs = frameMs,
                    AllocatedBytes = GC.GetTotalAllocatedBytes(false),
                    Gen2Count = GC.CollectionCount(2),
                    Screen = currentScreen?.GetType().Name ?? "",
                    UpdateMs = updateMsSinceLastRender,
                    RenderMs = (beforeSwap - renderStart) * TicksToMs,
                    SwapMs = (afterSwap - beforeSwap) * TicksToMs,
                    InputEvents = Interlocked.Exchange(ref inputEventsSinceLastRender, 0)
                };
            }

            updateMsSinceLastRender = 0;
        }

        // Writes frametimes_<time>.csv into the directory (created if needed). Does nothing if disabled or empty.
        public void WriteCsv(string directory)
        {
            if (!IsEnabled || count == 0) return;

            Directory.CreateDirectory(directory);   // does nothing if it already exists

            string path = Path.Combine(directory, $"frametimes_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            using var writer = new StreamWriter(path);
            writer.WriteLine("frame,frame_ms,allocated_bytes,gen2_count,screen,update_ms,render_ms,swap_ms,input_events");

            for (int i = 0; i < count; i++)
            {
                FrameSample s = samples[i];
                writer.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{i},{s.FrameMs:F4},{s.AllocatedBytes},{s.Gen2Count},{s.Screen},{s.UpdateMs:F4},{s.RenderMs:F4},{s.SwapMs:F4},{s.InputEvents}"));
            }

            Console.WriteLine($"[INFO] Wrote {count} frames to {path}");
        }
    }
}