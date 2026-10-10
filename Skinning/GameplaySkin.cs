namespace ProjectOdyssey.Skinning
{
    // A skin is a collection of resources that define the visual appearance of the game.
    // Each skin has a name, a directory where its resources are stored, and a list of gameplay skins for different key counts.
    public class Skin
    {
        public string Name { get; set; }
        public List<GameplaySkinConfiguration> GameplaySkins { get; set; } // Stores a list of different gameplay skins, one for each keycount
    }

    // Property initialisers are the defaults: System.Text.Json leaves a property
    // untouched when it's absent from config.json, so a skin that omits a field
    // gets the default instead of 0.
    public class GameplaySkinConfiguration
    {
        public byte KeyCount { get; set; }
        public int NoteWidth { get; set; } = 150;
        public int NoteHeight { get; set; } = 150;
        public int HitPositionX { get; set; } = 960;
        public int HitPositionY { get; set; } = 1000;
        public int ColumnSpacing { get; set; } = 0;
        public TargetType TargetType { get; set; } = TargetType.Receptor;
        public List<string>? TapNoteImage { get; set; } // By Column, points to the image used in the skin for tap notes
        public List<string>? LnHeadImage { get; set; } // By Column, points to the image used in the skin for long note heads
        public List<string>? LnBodyImage { get; set; } // Points to the image used in the skin for long note bodies
        public List<string>? LnTailImage { get; set; } // Points to the image used in the skin for long note tails
        public string? KeyDownReceptorImage { get; init; } // Points to the image used in the skin for receptors when a key is pressed
        public string? KeyUpReceptorImage { get; init; } // Points to the image used in the skin for receptors when a key is not pressed
        public string? JudgementLineImage { get; init; } // Points to the image used in the skin for the judgement line
        public string? JudgementMarvellousImage { get; set; } // Points to the image used in the skin for Marvellous judgements
        public string? JudgementPerfectImage { get; set; } // Points to the image used in the skin for Perfect judgements
        public string? JudgementGreatImage { get; set; } // Points to the image used in the skin for Great judgements
        public string? JudgementGoodImage { get; set; } // Points to the image used in the skin for Good judgements
        public string? JudgementBadImage { get; set; } // Points to the image used in the skin for Bad judgements
        public string? JudgementMissImage { get; set; } // Points to the image used in the skin for Miss judgements
    }

    public enum TargetType
    {
        Line,
        Receptor
    }
}
