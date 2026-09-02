using System.Text.Json;

namespace ProjectOdyssey
{
    public static class ChartFileReader
    {
        public static ChartData LoadChart(string file)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Charts", file);
            string json = File.ReadAllText(path);

            if (json == null)
            {
                Console.WriteLine("[Error] Could not load chart");
            }

            return JsonSerializer.Deserialize<ChartData>(json);
        }

        public static void WriteChart(ChartData chartData, string file)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Charts", file);
            string json = JsonSerializer.Serialize(chartData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
