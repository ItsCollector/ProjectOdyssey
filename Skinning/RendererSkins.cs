using ProjectOdyssey.Engine;
using ProjectOdyssey.Render;

namespace ProjectOdyssey.Skinning
{
    // Bundles of already-loaded resources handed to each renderer at construction.
    //
    // OWNERSHIP: the SkinManager owns every Texture and GlyphSet in here and is the
    // only thing that disposes them. Renderers borrow them and must never Dispose them.

    public sealed class ChartBrowserSkin
    {
        public required Texture MissingBackground { get; init; }
        public required Texture SetCard { get; init; }
        public required Texture SetCardHover { get; init; }
        public required Texture ChartCard { get; init; }
        public required Texture ChartCardHover { get; init; }
        public required GlyphSet Glyphs { get; init; }
    }

    public sealed class GameplaySkin
    {
        public required GameplaySkinConfig Config { get; init; }
        public required Texture[] TapNotes { get; init; }   // variants, at least 1
        public required Texture[] LnHeads { get; init; }    // variants, at least 1
        public required Texture LnBody { get; init; }
        public required Texture LnTail { get; init; }
        public required Texture JudgementLine { get; init; }
        public required Texture ReceptorUp { get; init; }
        public required Texture ReceptorDown { get; init; }
    }

    public sealed class HudSkin
    {
        public required IReadOnlyDictionary<JudgementType, Texture> Judgements { get; init; } // every JudgementType present
        public required GlyphSet Glyphs { get; init; }
    }
}
