using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProjectOdyssey.io
{
    public static class ChartLoader
    {
        public static ChartData LoadChart(string file)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "charts", file);
            string json = File.ReadAllText(path);

            if (json == null)
            {
                Console.WriteLine("[Error] Could not load chart");
            }

            return JsonSerializer.Deserialize<ChartData>(json);
        }
    }
}
