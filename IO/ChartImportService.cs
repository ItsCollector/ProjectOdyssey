using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey
{
    public class ChartImportService
    {
        // Default osu! stable install location
        private static readonly string OsuSongsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "osu!", "Songs");

        public static void ImportChartsFromOsu()
        {
            if (!Directory.Exists(OsuSongsRoot))
            {
                Console.WriteLine($"[Error] osu! Songs directory not found at {OsuSongsRoot}");
                return;
            }

            var chartDatabase = new ChartDatabase();

            foreach (var songDir in Directory.GetDirectories(OsuSongsRoot))
            {
                foreach (var chartFile in Directory.GetFiles(songDir, "*.osu"))
                {
                    var result = OsuChartParser.OsuToChartData(chartFile);

                    if (result.isSuccess)
                    {
                        var chartData = result.value;

                        // Insert into database
                    }
                }
            }
        }
    }
}
