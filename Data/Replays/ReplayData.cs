using ProjectOdyssey.Engine.Scoring;

namespace ProjectOdyssey.Data.Replays
{
    public class ReplayData
    {
        public int SetId { get; init; }
        public int ChartId { get; init; }
        public ReplayEvent[] ReplayEvents { get; init; } = Array.Empty<ReplayEvent>();
        public int[] ColumnByVKey { get; init; } = new int[256];
        public ScoreData ScoreData { get; init; }
    }
}
