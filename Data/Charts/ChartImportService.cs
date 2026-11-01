using ProjectOdyssey.Common;
using ProjectOdyssey.Engine.Chart;
using ProjectOdyssey.Screens.Import;
using System.Text;

namespace ProjectOdyssey.Data.Charts
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
                string error = $"[ERROR] osu! Songs directory not found at {osuSongsRoot}";
                Console.WriteLine(error);
                ImportProgress.Report(error, newRow: true);
                return;
            }

            Console.WriteLine($"[INFO] Starting import from osu! Songs directory: {osuSongsRoot}");
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

                    var parsedChartsInSet = new List<(ChartData chartData, string resolvedAudioPath, string binaryFilePath, string resolvedBackgroundPath)>();

                    foreach (var chartFile in Directory.GetFiles(songDir, "*.osu"))
                    {
                        var result = OsuChartParser.OsuToChartData(chartFile);
                        if (result.IsSuccess)
                        {
                            var (chartData, resolvedAudioPath, resolvedBackgroundPath) = result.Value;
                            ImportProgress.Report($"[{processedFolders}/{allFolders.Length}] Importing {chartData.Title} - {chartData.DiffName}", newRow: true);
                            string binaryFilePath = WriteChartBinary(chartData, songDir, chartFile);
                            parsedChartsInSet.Add((chartData, resolvedAudioPath, binaryFilePath, resolvedBackgroundPath));
                        }
                        else if (result.Error != "Unsupported mode")
                        {
                            string error = $"[WARN] Failed to parse {chartFile}: {result.Error}";
                            errorLog.AppendLine(error);
                            ImportProgress.Report(error, newRow: false);
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
                    string error = $"[ERROR] Failed to import set '{songDir}': {ex.Message}";
                    errorLog.AppendLine(error);
                    ImportProgress.Report(error, newRow: true);
                }

                if (processedFolders % 100 == 0)
                {
                    string progress = $"[PROGRESS] {processedFolders}/{allFolders.Length} folders scanned, {importedCount} sets imported";
                    Console.WriteLine(progress);
                    ImportProgress.Report(progress, newRow: true);
                }
            }

            string logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);

            string logPath = Path.Combine(logDir, $"import_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            if (errorLog.Length > 0)
            {
                File.WriteAllText(logPath, errorLog.ToString());
            }

            string message = $"[PROGRESS] Imported {importedCount} sets. Log written to {logPath} if any errors";
            Console.WriteLine(message);
            ImportProgress.Report(message, newRow: true);
        }

        public static void InsertItem(string folderPath, List<(ChartData chartData, string resolvedAudioPath, string jsonFilePath, string backgroundPath)> parsedChartsInSet)
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