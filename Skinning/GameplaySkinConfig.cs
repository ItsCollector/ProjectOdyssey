namespace ProjectOdyssey.Skinning
{
    // Property initialisers are the defaults: System.Text.Json leaves a property
    // untouched when it's absent from config.json, so a skin that omits a field
    // gets the default instead of 0.
    public class GameplaySkinConfig
    {
        public int NoteWidth { get; set; } = 150;
        public int NoteHeight { get; set; } = 150;
        public int HitPositionX { get; set; } = 960;
        public int HitPositionY { get; set; } = 1000;
        public int ColumnSpacing { get; set; } = 0;
        public TargetType TargetType { get; set; } = TargetType.Receptor;
    }

    public enum TargetType
    {
        Line,
        Receptor
    }
}
