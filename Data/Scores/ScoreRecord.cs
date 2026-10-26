using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Data.Scores
{
    public class ScoreRecord
    {
        public int ScoreId { get; set; }
        public int SetId { get; set; }
        public int ChartId { get; set; }
        public float Accuracy { get; set; }
        public int MarvCount { get; set; }
        public int PerfCount { get; set; }
        public int GreatCount { get; set; }
        public int GoodCount { get; set; }
        public int BadCount { get; set; }
        public int MissCount { get; set; }
        public int HighestCombo { get; set; }
        public int PauseCount { get; set; }
        public string ReplayPath { get; set; } = "";
        public long PlayedUtc { get; set; }
    }
}
