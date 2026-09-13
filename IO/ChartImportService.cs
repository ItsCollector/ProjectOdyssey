using System.Text.Json;

namespace ProjectOdyssey.IO
{
    public static class ChartImportService
    {
        private static readonly string OsuSongsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "osu!", "Songs");

        private static ChartDatabase chartDatabase = new ChartDatabase();

        public static void ImportChartsFromOsu(int maxSets = 10)
        {
            if (!Directory.Exists(OsuSongsRoot))
            {
                Console.WriteLine($"[Error] osu! Songs directory not found at {OsuSongsRoot}");
                return;
            }

            int importedCount = 0;

            foreach (var songDir in Directory.GetDirectories(OsuSongsRoot))
            {
                if (importedCount >= maxSets)
                {
                    break;
                }

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
                        string jsonFilePath = WriteChartJson(songDir, chartData);
                        parsedChartsInSet.Add((chartData, resolvedAudioPath, jsonFilePath));
                    }
                    else
                    {
                        Console.WriteLine($"[WARN] Failed to parse {chartFile}: {result.error}");
                    }
                }

                if (parsedChartsInSet.Count > 0)
                {
                    InsertItem(songDir, parsedChartsInSet);
                    importedCount++;
                }
            }
        }

        public static void InsertItem(string folderPath, List<(ChartData chartData, string resolvedAudioPath, string jsonFilePath)> parsedChartsInSet)
        {
            int setId = chartDatabase.InsertChartSet(new ChartSet
            {
                FolderPath = folderPath,
                Source = "OsuLink"
            });

            foreach (var (chartData, resolvedAudioPath, jsonFilePath) in parsedChartsInSet)
            {
                int songId = chartDatabase.GetOrCreateSong(new SongRecord
                {
                    SetId = setId,
                    AudioPath = resolvedAudioPath,
                    Title = chartData.title,
                    Artist = chartData.artist
                });

                chartDatabase.InsertChart(new ChartRecord
                {
                    SongId = songId,
                    FilePath = jsonFilePath,
                    DiffName = chartData.diffName,
                    Noter = chartData.noter,
                    KeyCount = chartData.keyCount,
                    FileLastWriteUtc = DateTime.UtcNow.Ticks
                });
            }
        }

        private static string WriteChartJson(string sourceFolderPath, ChartData chartData)
        {
            string chartsRoot = Path.Combine(AppContext.BaseDirectory, "Charts");
            string setFolderName = Path.GetFileName(sourceFolderPath.TrimEnd(Path.DirectorySeparatorChar));
            string destinationFolder = Path.Combine(chartsRoot, setFolderName);

            Directory.CreateDirectory(destinationFolder);

            string fileName = $"{chartData.diffName}.json";
            string outputPath = Path.Combine(destinationFolder, fileName);

            string json = JsonSerializer.Serialize(chartData);
            File.WriteAllText(outputPath, json);

            return outputPath;
        }
    }
}