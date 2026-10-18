using ProjectOdyssey.Input;
using ProjectOdyssey.Engine.Scoring;

namespace ProjectOdyssey.IO
{
    public class ReplayData
    {
        public ReplayEvent[] ReplayEvents { get; init; } = Array.Empty<ReplayEvent>();
        public int[] ColumnByVKey { get; init; } = new int[256];
        public ScoreData ScoreData { get; init; }
    }
}
