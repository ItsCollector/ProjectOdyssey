using ProjectOdyssey.Engine;
using ProjectOdyssey.Render;

namespace ProjectOdyssey.Skinning
{
    // Bundles of already-loaded resources handed to each renderer at construction.
    //
    // OWNERSHIP: the SkinManager owns every Texture and GlyphSet in here and is the
    // only thing that disposes them. Renderers borrow them and must never Dispose them.

    public sealed class MenuSkin
    {
        public required Texture MissingBackground { get; init; }
        public required Texture SetCard { get; init; }
        public required Texture SetCardHover { get; init; }
        public required Texture ChartCard { get; init; }
        public required Texture ChartCardHover { get; init; }
        public required Texture PausedBackground { get; init; }
        public required GlyphSet Glyphs { get; init; }
    }

    public sealed class GameplaySkin
    {
        public byte KeyCount { get; set; }
        public int NoteWidth { get; set; }
        public int NoteHeight { get; set; }
        public int HitPositionX { get; set; }
        public int HitPositionY { get; set; }
        public int ColumnSpacing { get; set; }
        public TargetType TargetType { get; set; }
        public required Texture[] TapNotes { get; init; }   
        public required Texture[] LnHeads { get; init; }    
        public required Texture[] LnBodies { get; init; }  
        public required Texture[] LnTails { get; init; }   
        public Texture? JudgementLine { get; init; }
        public Texture? ReceptorUp { get; init; }
        public Texture? ReceptorDown { get; init; }
    }

    public sealed class HudSkin
    {
        public required IReadOnlyDictionary<JudgementType, Texture> Judgements { get; init; } // every JudgementType present
        public required GlyphSet Glyphs { get; init; }
    }
}
