using ProjectOdyssey.Engine;
using System.Text;

namespace ProjectOdyssey.IO
{
    public static class ChartImportService
    {
        private static readonly string OsuSongsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "osu!", "Songs");

        private static ChartDatabase chartDatabase = new ChartDatabase();

        public static void ImportChartsFromOsu(int maxSets = int.MaxValue)
        {
            if (!Directory.Exists(OsuSongsRoot))
            {
                Console.WriteLine($"[Error] osu! Songs directory not found at {OsuSongsRoot}");
                return;
            }

            Console.WriteLine($"[Info] Starting import from osu! Songs directory: {OsuSongsRoot}");
            int importedCount = 0;
            var errorLog = new StringBuilder();
            var allFolders = Directory.GetDirectories(OsuSongsRoot);
            int processedFolders = 0;

            foreach (var songDir in allFolders)
            {
                processedFolders++;

                if (importedCount >= maxSets)
                {
                    break;
                }

                try
                {
                    if (chartDatabase.ChartSetExists(songDir)) // skip imported sets
                    {
                        continue;
                    }

                    var parsedChartsInSet = new List<(ChartData chartData, string resolvedAudioPath, string jsonFilePath)>();

                    foreach (var chartFile in Directory.GetFiles(songDir, "*.osu"))
                    {
                        var result = OsuChartParser.OsuToChartData(chartFile);
                        if (result.isSuccess)
                        {
                            var (chartData, resolvedAudioPath) = result.value;
                            string binaryFilePath = WriteChartBinary(chartData, songDir, chartFile);
                            parsedChartsInSet.Add((chartData, resolvedAudioPath, binaryFilePath));
                        }
                        else if (result.error != "Unsupported mode")
                        {
                            errorLog.AppendLine($"[WARN] Failed to parse {chartFile}: {result.error}");
                        }
                    }

                    if (parsedChartsInSet.Count > 0)
                    {
                        InsertItem(songDir, parsedChartsInSet);
                        importedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorLog.AppendLine($"[ERROR] Failed to import set '{songDir}': {ex.Message}");
                }

                if (processedFolders % 100 == 0)
                {
                    Console.WriteLine($"[Progress] {processedFolders}/{allFolders.Length} folders scanned, {importedCount} sets imported");
                }
            }

            string logPath = Path.Combine(AppContext.BaseDirectory, $"import_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(logPath, errorLog.ToString());

            Console.WriteLine($"[Done] Imported {importedCount} sets. Log written to {logPath}");
        }

        public static void InsertItem(string folderPath, List<(ChartData chartData, string resolvedAudioPath, string jsonFilePath)> parsedChartsInSet)
        {
            chartDatabase.ImportChartSet(new ChartSet
            {
                FolderPath = folderPath,
                Source = "OsuLink"
            }, parsedChartsInSet);
        }

        private static string SanitizeForFileSystem(string name)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidChar, '_');
            }
            return name;
        }

        public static string WriteChartBinary(ChartData chartData, string sourceFolderPath, string sourceOsuFilePath)
        {
            string chartsRoot = Path.Combine(AppContext.BaseDirectory, "Charts");
            string setFolderName = SanitizeForFileSystem(Path.GetFileName(sourceFolderPath.TrimEnd(Path.DirectorySeparatorChar)));
            string destinationFolder = Path.Combine(chartsRoot, setFolderName);

            Directory.CreateDirectory(destinationFolder);

            string baseName = SanitizeForFileSystem(Path.GetFileNameWithoutExtension(sourceOsuFilePath));
            string outputPath = Path.Combine(destinationFolder, $"{baseName}.chart");

            using (var writer = new BinaryWriter(File.Open(outputPath, FileMode.Create)))
            {
                writer.Write((byte)1); // format version
                writer.Write(chartData.title);
                writer.Write(chartData.artist);
                writer.Write(chartData.noter);
                writer.Write(chartData.diffName);
                writer.Write(chartData.keyCount);

                for (int col = 0; col < chartData.keyCount; col++)
                {
                    writer.Write(chartData.notesByColumn[col].Length);
                    foreach (var note in chartData.notesByColumn[col])
                    {
                        WriteNote(writer, note);
                    }
                }
            }

            return outputPath;
        }

        public static void WriteNote(BinaryWriter writer, Note note)
        {
            writer.Write((byte)note.noteType);
            writer.Write(note.column);
            writer.Write(note.startTime);
            writer.Write(note.endTime);
        }
    }
}