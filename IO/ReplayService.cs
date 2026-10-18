using ProjectOdyssey.Engine;
using ProjectOdyssey.Engine.Scoring;
using ProjectOdyssey.Input;

namespace ProjectOdyssey.IO
{
    public static class ReplayService
    {
        // File layout (version 1), all values little-endian via BinaryWriter:
        //
        //   byte    format version
        //   long    set id
        //   long    chart id
        //   long    SetTimeUtc
        //   float   continuous accuracy
        //   float   discrete accuracy
        //   int     highest combo
        //   int     pause count
        //   byte    key count (N)
        //   ushort  x N     VKey bound to each column, in column order
        //   int     x M     judgement counts, one per JudgementType in enum order
        //   int     event count (E)
        //   E x { float songTimeMs, ushort vKey, bool isPressed }   (7 bytes each)
        //
        // Keep the field types below in sync with ScoreData if they differ from the above.

        public const string FileExtension = ".odr";
        private const byte FormatVersion = 1;

        private static readonly string replayFolder = Path.Combine(AppContext.BaseDirectory, "Replays");

        public static string WriteReplayBinary(ReplayData replayData)
        {
            ScoreData scoreData = replayData.ScoreData;
            ushort[] bindsByColumn = BuildBindsByColumn(replayData.ColumnByVKey);

            Directory.CreateDirectory(replayFolder);
            string outputPath = GetUniquePath(replayData.SetId, replayData.ChartId, scoreData.SetTimeUtc);

            using (var writer = new BinaryWriter(File.Open(outputPath, FileMode.CreateNew)))
            {
                // Header
                writer.Write(FormatVersion);
                writer.Write(replayData.SetId);
                writer.Write(replayData.ChartId);
                writer.Write(scoreData.SetTimeUtc);
                writer.Write(scoreData.CurrentContinuousAccuracy);
                writer.Write(scoreData.CurrentDiscreteAccuracy);
                writer.Write(scoreData.HighestCombo);
                writer.Write(scoreData.PauseCount);

                // Gameplay bindings: one VKey per column, so the reader can rebuild the VKey -> column table
                writer.Write((byte)bindsByColumn.Length);
                foreach (ushort vKey in bindsByColumn)
                {
                    writer.Write(vKey);
                }

                // Judgement counts (fixed enum order, missing types written as 0)
                foreach (JudgementType type in Enum.GetValues<JudgementType>())
                {
                    writer.Write(scoreData.JudgementCounts.GetValueOrDefault(type));
                }

                // Replay events, prefixed with a count so the reader knows how many to read
                writer.Write(replayData.ReplayEvents.Length);
                foreach (ReplayEvent replayEvent in replayData.ReplayEvents)
                {
                    writer.Write(replayEvent.SongTimeMs);
                    writer.Write(replayEvent.VKey);
                    writer.Write(replayEvent.IsPressed);
                }
            }

            return outputPath;
        }

        // ColumnByVKey is a VKey -> column table (-1 = unbound). Invert it to column -> VKey.
        private static ushort[] BuildBindsByColumn(int[] columnByVKey)
        {
            int keyCount = 0;
            foreach (int column in columnByVKey)
            {
                if (column + 1 > keyCount)
                {
                    keyCount = column + 1;
                }
            }

            if (keyCount == 0)
            {
                throw new InvalidOperationException("Cannot write a replay with no bound keys.");
            }

            if (keyCount > byte.MaxValue)
            {
                throw new InvalidOperationException($"Replay has {keyCount} columns, which doesn't fit in the file format.");
            }

            var binds = new ushort[keyCount];

            for (int vKey = 0; vKey < columnByVKey.Length; vKey++)
            {
                int column = columnByVKey[vKey];

                if (column >= 0)
                {
                    binds[column] = (ushort)vKey;
                }
            }

            return binds;
        }

        // Never overwrite an existing replay: two plays with the same timestamp get a numeric suffix.
        private static string GetUniquePath(long setId, long chartId, long playedUtc)
        {
            string baseName = $"REPLAY_{setId}_{chartId}_{playedUtc}";
            string path = Path.Combine(replayFolder, baseName + FileExtension);

            for (int attempt = 1; File.Exists(path); attempt++)
            {
                path = Path.Combine(replayFolder, $"{baseName}_{attempt}{FileExtension}");
            }

            return path;
        }
    }
}