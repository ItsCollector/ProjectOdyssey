namespace ProjectOdyssey
{
    // Holds the resolved file paths for every skin component, once discovery succeeds.
    public class SkinAssets
    {
        public required string[] TapNotePaths { get; init; }
        public required string[] LnHeadPaths { get; init; }
        public required string LnBodyPath { get; init; }
        public required string LnTailPath { get; init; }
        public required string JudgementLinePath { get; init; }
        public required string ReceptorUpPath { get; init; }
        public required string ReceptorDownPath { get; init; }
    }

}
