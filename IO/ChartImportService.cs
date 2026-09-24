using ProjectOdyssey.Engine;
using System.Text;

namespace ProjectOdyssey.IO
{
    public static class ChartImportService
    {
        private static readonly string osuSongsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "osu!", "Songs");

        public static void ImportChartsFromOsu(int maxSets = int.MaxValue)
        {
            if (!Directory.Exists(osuSongsRoot))
            {
                Console.WriteLine($"[Error] osu! Songs directory not found at {osuSongsRoot}");
                return;
            }

            Console.WriteLine($"[Info] Starting import from osu! Songs directory: {osuSongsRoot}");
            int importedCount = 0;
            var errorLog = new StringBuilder();
            var allFolders = Directory.GetDirectories(osuSongsRoot);
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
                    if (ChartDatabase.ChartSetExists(songDir)) // skip imported sets
                    {
                        continue;
                    }

                    var parsedChartsInSet = new List<(ChartData chartData, string resolvedAudioPath, string jsonFilePath)>();

                    foreach (var chartFile in Directory.GetFiles(songDir, "*.osu"))
                    {
                        var result = OsuChartParser.OsuToChartData(chartFile);
                        if (result.IsSuccess)
                        {
                            var (chartData, resolvedAudioPath) = result.Value;
                            string binaryFilePath = WriteChartBinary(chartData, songDir, chartFile);
                            parsedChartsInSet.Add((chartData, resolvedAudioPath, binaryFilePath));
                        }
                        else if (result.Error != "Unsupported mode")
                        {
                            errorLog.AppendLine($"[WARN] Failed to parse {chartFile}: {result.Error}");
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
            ChartDatabase.ImportChartSet(new ChartSet
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
                writer.Write(chartData.Title);
                writer.Write(chartData.Artist);
                writer.Write(chartData.Noter);
                writer.Write(chartData.DiffName);
                writer.Write(chartData.KeyCount);
                for (int col = 0; col < chartData.KeyCount; col++)
                {
                    writer.Write(chartData.NotesByColumn[col].Length);
                    foreach (var note in chartData.NotesByColumn[col])
                    {
                        WriteNote(writer, note);
                    }
                }
            }

            return outputPath;
        }

        public static void WriteNote(BinaryWriter writer, Note note)
        {
            writer.Write((byte)note.NoteType);
            writer.Write(note.Column);
            writer.Write(note.StartTime);
            writer.Write(note.EndTime);
        }
    }
}