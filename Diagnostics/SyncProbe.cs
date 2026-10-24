using System.Globalization;

namespace ProjectOdyssey.Diagnostic
{
    // Diagnostic only. Compares the game clock (a Stopwatch) with the audio device's own playback position,
    // once per second of audio, and reports:
    //   - the start offset: how far apart the two were when the song began (a fixed offset, fixed by globalOffsetMs)
    //   - the drift: how much further apart they pulled over the song (needs a different fix)
    //
    // diff = game - audio.  Positive means the game clock is ahead of the music, i.e. notes arrive earlier than they should.
    public sealed class SyncProbe
    {
        private const double SampleEveryMs = 1000;

        private readonly List<(double GameMs, double AudioMs)> samples = new();
        private double nextSampleAudioMs = SampleEveryMs; // skip the first second: the audio device is still starting up

        public bool IsEnabled { get; }

        public SyncProbe(bool isEnabled = true)
        {
            IsEnabled = isEnabled;
        }

        // Call every frame once audio has started. Cheap: it only records one sample per second of audio.
        public void Sample(double gameSongMs, double audioPositionMs)
        {
            if (!IsEnabled) return;
            if (audioPositionMs < nextSampleAudioMs) return;

            samples.Add((gameSongMs, audioPositionMs));
            nextSampleAudioMs = audioPositionMs + SampleEveryMs;
        }

        // Writes sync_<time>.csv into the directory and prints a summary. Does nothing for very short plays.
        public void WriteReport(string directory)
        {
            if (!IsEnabled) return;
            if (samples.Count < 3) return;

            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"sync_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("sample,audio_ms,game_ms,diff_ms");

                for (int i = 0; i < samples.Count; i++)
                {
                    var (game, audio) = samples[i];
                    writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{i},{audio:F2},{game:F2},{game - audio:F2}"));
                }
            }

            // Least-squares slope of (game - audio) against audio time = how fast the two clocks pull apart
            double n = samples.Count, sumX = 0, sumY = 0, sumXX = 0, sumXY = 0;
            double largestJump = 0, previousDiff = samples[0].GameMs - samples[0].AudioMs;

            foreach (var (game, audio) in samples)
            {
                double diff = game - audio;
                sumX += audio;
                sumY += diff;
                sumXX += audio * audio;
                sumXY += audio * diff;

                largestJump = Math.Max(largestJump, Math.Abs(diff - previousDiff));
                previousDiff = diff;
            }

            double slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX); // ms of drift per ms of audio
            double spanMs = samples[^1].AudioMs - samples[0].AudioMs;
            double startOffset = samples[0].GameMs - samples[0].AudioMs;

            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"[SYNC] {samples.Count} samples over {spanMs / 1000:F0} s. Start offset: {startOffset:+0.0;-0.0} ms (positive = game ahead of audio)."));
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"[SYNC] Drift: {slope * spanMs:+0.0;-0.0} ms over the song ({slope * 1e6:+0;-0} ppm). Largest jump between samples: {largestJump:F1} ms. Saved {path}"));
        }
    }
}