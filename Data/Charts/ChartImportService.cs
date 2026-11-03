using ProjectOdyssey.Common;
using ProjectOdyssey.Engine.Chart;
using ProjectOdyssey.Screens.Import;
using System.Security.Cryptography;

namespace ProjectOdyssey.Data.Charts
{
    public static class ChartImportService
    {
        private static readonly string osuSongsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "osu!", "Songs");

        // A folder that contains at least one .osu file we haven't seen before (not imported, not rejected).
        private sealed class PendingSet
        {
            public string Folder = "";
            public List<(string File, string Md5)> Files = new();
        }

        // Appends to the log as problems happen, so a crash or closed game doesn't lose it.
        // The file is only created if something is actually written.
        private sealed class ImportLog : IDisposable
        {
            private readonly string logPath = Path.Combine(
                AppContext.BaseDirectory, "Logs", $"import_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            private StreamWriter? writer;

            public int Count { get; private set; }

            public void Write(string line)
            {
                Console.WriteLine(line);

                try
                {
                    if (writer == null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                        writer = new StreamWriter(logPath, append: false) { AutoFlush = true };
                    }
                    writer.WriteLine(line);
                }
                catch
                {
                    // Logging must never take the import down with it
                }

                Count++;
            }

            public void Dispose() => writer?.Dispose();
        }

        public static void ImportChartsFromOsu(int maxSets = int.MaxValue)
        {
            if (!Directory.Exists(osuSongsRoot))
            {
                string error = $"osu! Songs directory not found at {osuSongsRoot}";
                Console.WriteLine($"[ERROR] {error}");
                ImportProgress.Report("Could not find the osu! Songs folder.", newRow: true);
                return;
            }

            Console.WriteLine($"[INFO] Starting import from osu! Songs directory: {osuSongsRoot}");

            using var log = new ImportLog();
            var rejected = new List<(string md5Hash, string reason)>();

            void FlushRejected()
            {
                if (rejected.Count == 0) return;

                try
                {
                    ChartDatabase.AddRejectedCharts(rejected);
                    rejected.Clear();
                }
                catch (Exception ex)
                {
                    log.Write($"[WARN] Could not save rejected chart hashes: {ex.Message}");
                }
            }

            // Pass 1: scan 
            // Hash every .osu file and keep only the folders with something new. This gives the progress
            // display an honest total, and files from other game modes are rejected here without being parsed.
            HashSet<string> known = ChartDatabase.GetAllMD5(); // imported charts + rejected charts
            string[] allFolders = Directory.GetDirectories(osuSongsRoot);
            var pendingSets = new List<PendingSet>();

            ImportProgress.Report($"Scanning folders (0/{allFolders.Length})", newRow: true);

            for (int i = 0; i < allFolders.Length; i++)
            {
                if (pendingSets.Count >= maxSets)
                {
                    break;
                }

                if (i % 25 == 0)
                {
                    ImportProgress.Report($"Scanning folders ({i}/{allFolders.Length})", newRow: false);
                }

                PendingSet? pending = null;

                foreach (string chartFile in Directory.GetFiles(allFolders[i], "*.osu"))
                {
                    try
                    {
                        string md5Hash = CalculateMD5(chartFile);

                        // Add returns false if we already have it: imported, rejected, or the same file
                        // showing up in another folder earlier in this run.
                        if (!known.Add(md5Hash))
                        {
                            continue;
                        }

                        if (OsuChartParser.IsUnsupportedMode(chartFile))
                        {
                            rejected.Add((md5Hash, OsuChartParser.UnsupportedModeError));
                            continue;
                        }

                        pending ??= new PendingSet { Folder = allFolders[i] };
                        pending.Files.Add((chartFile, md5Hash));
                    }
                    catch (Exception ex)
                    {
                        log.Write($"[WARN] Could not scan {chartFile}: {ex.Message}");
                    }
                }

                if (pending != null)
                {
                    pendingSets.Add(pending);
                }
            }

            FlushRejected();

            if (pendingSets.Count == 0)
            {
                ImportProgress.Report("Nothing new to import.", newRow: true);
                Console.WriteLine("[INFO] Nothing new to import.");
                return;
            }

            ImportProgress.Report($"Found {pendingSets.Count} sets to import.", newRow: true);

            // Pass 2: import 
            int importedSets = 0;
            int importedCharts = 0;
            int failedSets = 0;

            for (int s = 0; s < pendingSets.Count; s++)
            {
                PendingSet set = pendingSets[s];
                var parsedChartsInSet = new List<(ChartData chartData, string resolvedAudioPath, string binaryFilePath, string resolvedBackgroundPath, string md5Hash)>();
                var newBinaries = new List<string>(); // binaries this set created, so a failure can remove them
                bool reportedSet = false;

                try
                {
                    foreach (var (chartFile, md5Hash) in set.Files)
                    {
                        Result<(ChartData chartData, string resolvedAudioPath, string resolvedBackgroundPath)> result;

                        try
                        {
                            result = OsuChartParser.OsuToChartData(chartFile);
                        }
                        catch (Exception ex)
                        {
                            // A malformed file must only cost us that difficulty, not the whole set
                            log.Write($"[WARN] Failed to parse {chartFile}: {ex.Message}");
                            continue;
                        }

                        if (!result.IsSuccess)
                        {
                            if (result.Error == OsuChartParser.UnsupportedModeError)
                            {
                                rejected.Add((md5Hash, OsuChartParser.UnsupportedModeError));
                            }
                            else
                            {
                                log.Write($"[WARN] Failed to parse {chartFile}: {result.Error}");
                            }
                            continue;
                        }

                        var (chartData, resolvedAudioPath, resolvedBackgroundPath) = result.Value;

                        if (!reportedSet)
                        {
                            // One row per set, not per difficulty
                            ImportProgress.Report($"Importing {chartData.Title} ({s + 1}/{pendingSets.Count})", newRow: true);
                            reportedSet = true;
                        }

                        string binaryPath = GetChartBinaryPath(set.Folder, chartFile);
                        bool binaryAlreadyExisted = File.Exists(binaryPath);

                        string binaryFilePath = WriteChartBinary(chartData, set.Folder, chartFile);

                        if (!binaryAlreadyExisted)
                        {
                            newBinaries.Add(binaryFilePath);
                        }

                        parsedChartsInSet.Add((chartData, resolvedAudioPath, binaryFilePath, resolvedBackgroundPath, md5Hash));
                    }

                    if (parsedChartsInSet.Count > 0)
                    {
                        InsertItem(set.Folder, parsedChartsInSet);
                        importedSets++;
                        importedCharts += parsedChartsInSet.Count;
                    }
                }
                catch (Exception ex)
                {
                    failedSets++;
                    log.Write($"[ERROR] Failed to import set '{set.Folder}': {ex.Message}");

                    // The DB transaction rolled back, so the binaries written for this set belong to nothing.
                    // Files that existed before this run (an edited map being refreshed) are left alone.
                    foreach (string orphan in newBinaries)
                    {
                        try 
                        { 
                            File.Delete(orphan); 
                        } 
                        catch 
                        { 
                            log.Write($"[WARN] Could not delete orphaned binary: {orphan}");
                        }
                    }
                }

                FlushRejected();
            }

            string summary = $"Done - imported {importedSets} sets ({importedCharts} charts).";
            Console.WriteLine($"[PROGRESS] {summary}");
            ImportProgress.Report(summary, newRow: true);

            if (failedSets > 0)
            {
                ImportProgress.Report($"{failedSets} sets failed to import.", newRow: true);
            }

            if (log.Count > 0)
            {
                ImportProgress.Report($"{log.Count} problems were written to the Logs folder.", newRow: true);
            }
        }

        public static void InsertItem(string folderPath, List<(ChartData chartData, string resolvedAudioPath, string binaryFilePath, string backgroundPath, string md5Hash)> parsedChartsInSet)
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

        // Where the binary for a given .osu file will be written. Does not touch the disk.
        public static string GetChartBinaryPath(string sourceFolderPath, string sourceOsuFilePath)
        {
            string chartsRoot = Path.Combine(AppContext.BaseDirectory, "Charts");
            string setFolderName = SanitizeForFileSystem(Path.GetFileName(sourceFolderPath.TrimEnd(Path.DirectorySeparatorChar)));
            string baseName = SanitizeForFileSystem(Path.GetFileNameWithoutExtension(sourceOsuFilePath));

            return Path.Combine(chartsRoot, setFolderName, $"{baseName}.chart");
        }

        public static string WriteChartBinary(ChartData chartData, string sourceFolderPath, string sourceOsuFilePath)
        {
            string outputPath = GetChartBinaryPath(sourceFolderPath, sourceOsuFilePath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

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

        public static string CalculateMD5(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}