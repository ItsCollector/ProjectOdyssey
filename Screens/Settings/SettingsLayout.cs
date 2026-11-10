namespace ProjectOdyssey.Screens.Settings
{
    public enum SettingKind { Toggle, Dropdown, NumberField, Submenu }

    public static class SettingIds
    {
        public const string DisplayMode = "display_mode";
        public const string Resolution = "resolution";
        public const string VSync = "vsync";
        public const string MaxFps = "max_fps";
        public const string Volume = "volume";
        public const string AudioOffset = "audio_offset";
        public const string GameplayBindings = "gameplay_bindings";
    }

    public sealed record SettingDefinition(
        string Id,
        string Label,
        SettingKind Kind,
        string? Suffix = null, // shown after number fields, e.g. "%" or "ms"
        double Min = 0, // number field limits
        double Max = 0,
        string? DisabledWhenOn = null // id of a toggle; this row is greyed out while that toggle is on
    );

    public sealed record SettingsSection(string Title, IReadOnlyList<SettingDefinition> Settings);

    // One line in the stack: either a section header or a setting.
    public sealed record SettingsEntry(bool IsHeader, string Text, SettingDefinition? Setting);

    public class SettingsLayout
    {
        // Panel docked to the left edge, full height
        public const int PanelWidth = 600, SubPanelWidth = 560;
        public const int PanelPadding = 32;
        public const int TitleHeight = 100;

        // Stack rows (stored in StackItem.Kind)
        public const int HeaderKind = 0, RowKind = 1;
        public const int HeaderHeight = 40, HeaderSpacing = 28;
        public const int RowHeight = 64, RowSpacing = 6;

        // Controls live in a column on the right-hand side of each row
        public const int ControlWidth = 240, ControlHeight = 44, CheckBoxSize = 40;

        public static float RowX => PanelPadding;
        public static int RowWidth => PanelWidth - PanelPadding * 2;
        public static float ControlX => PanelWidth - PanelPadding - ControlWidth;

        // Pass this to new StackView(...) so the first entry (a header) starts just under the title.
        public static float FirstRowCenterY => TitleHeight + HeaderHeight / 2f;

        // openAmount: 0 = hidden, 1 = fully open (feed it an eased value for the slide-out)
        public static float PanelX(float openAmount) => -PanelWidth * (1f - openAmount);
        public static float SubPanelX(float openAmount) => PanelX(openAmount) + PanelWidth;

        public static readonly IReadOnlyList<SettingsSection> Sections = new[]
        {
            new SettingsSection("Graphics", new[]
            {
                new SettingDefinition(SettingIds.DisplayMode, "Display mode", SettingKind.Dropdown),
                new SettingDefinition(SettingIds.Resolution, "Resolution", SettingKind.Dropdown),
                new SettingDefinition(SettingIds.VSync, "V-Sync", SettingKind.Toggle),
                new SettingDefinition(SettingIds.MaxFps, "Max FPS", SettingKind.NumberField, Suffix: "fps", Min: 30, Max: 1000, DisabledWhenOn: SettingIds.VSync),
            }),
            new SettingsSection("Audio", new[]
            {
                new SettingDefinition(SettingIds.Volume, "Volume", SettingKind.NumberField, Suffix: "%", Min: 0, Max: 100),
                new SettingDefinition(SettingIds.AudioOffset, "Audio offset", SettingKind.NumberField, Suffix: "ms", Min: -500, Max: 500),
            }),
            new SettingsSection("Input", new[]
            {
                new SettingDefinition(SettingIds.GameplayBindings, "Gameplay bindings", SettingKind.Submenu),
            }),
        };

        public static List<SettingsEntry> BuildEntries()
        {
            var entries = new List<SettingsEntry>();

            foreach (var section in Sections)
            {
                entries.Add(new SettingsEntry(true, section.Title, null));

                foreach (var setting in section.Settings)
                {
                    entries.Add(new SettingsEntry(false, setting.Label, setting));
                }
            }
            return entries;
        }
    }
}