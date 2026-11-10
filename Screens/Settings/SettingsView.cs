using OpenTK.Mathematics;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.Screens.Components;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens.Settings
{
    // What the view needs to know about current values. The screen implements this by reading your config.
    public interface ISettingsValues
    {
        bool GetBool(string id);
        string GetText(string id);                   // text shown in dropdowns and number fields
        bool IsDisabled(SettingDefinition setting);  // e.g. setting.DisabledWhenOn != null && GetBool(setting.DisabledWhenOn)
    }

    public class SettingsView
    {
        private const float ScreenHeight = 1080f;

        private readonly Renderer renderer;
        private readonly MenuSkin menuSkin;

        private readonly Vector4 primaryTextColour = new(1f, 1f, 1f, 1f);
        private readonly Vector4 accentTextColour = new(0.7765f, 0.7216f, 0.9569f, 1f);
        private readonly Vector4 secondaryTextColour = new(0.6667f, 0.6863f, 0.7451f, 1f);

        private readonly Vector4 panelColour = new(0.07f, 0.09f, 0.18f, 0.96f);
        private readonly Vector4 rowHoverColour = new(0.10f, 0.13f, 0.25f, 0.9f);
        private readonly Vector4 fieldColour = new(0.04f, 0.06f, 0.13f, 1f);
        private readonly Vector4 editBorderColour = new(0.21f, 0.88f, 1f, 1f);

        public SettingsView(MenuSkin menuSkin, Renderer renderer)
        {
            this.menuSkin = menuSkin;
            this.renderer = renderer;
        }

        public void DrawSettingsScreen(
            IReadOnlyList<StackItem> items,
            IReadOnlyList<SettingsEntry> entries,
            ISettingsValues values,
            float openAmount, // 0 = hidden, 1 = fully open
            StackItem? hovered,
            string? editingId, // id of the number field being typed into, or null
            string editText)
        {
            var glyphs = menuSkin.Glyphs;
            float panelX = SettingsLayout.PanelX(openAmount);

            // Panel background and title
            renderer.DrawQuad(panelX + SettingsLayout.PanelWidth / 2f, ScreenHeight / 2f, SettingsLayout.PanelWidth, ScreenHeight, panelColour);
            renderer.DrawText(glyphs, "Settings", panelX + SettingsLayout.PanelPadding, (SettingsLayout.TitleHeight - glyphs.FontSize) / 2f, primaryTextColour);

            foreach (var item in items)
            {
                var entry = entries[item.Tag];
                float x = panelX + item.X;
                float centreY = item.Y + item.Height / 2f; // item.Y already includes SpacingBefore
                float textY = item.Y + (item.Height - glyphs.FontSize) / 2f;

                if (entry.IsHeader)
                {
                    renderer.DrawText(glyphs, entry.Text.ToUpperInvariant(), x, textY, accentTextColour);
                    continue;
                }

                var setting = entry.Setting!;
                bool disabled = values.IsDisabled(setting);

                if (item == hovered && !disabled)
                {
                    renderer.DrawQuad(x + item.Width / 2f, centreY, item.Width, item.Height, rowHoverColour);
                }

                renderer.DrawText(glyphs, entry.Text, x + 12, textY, disabled ? secondaryTextColour : primaryTextColour);

                float controlLeft = panelX + SettingsLayout.ControlX;
                float controlRight = controlLeft + SettingsLayout.ControlWidth;
                var valueColour = disabled ? secondaryTextColour : primaryTextColour;

                switch (setting.Kind)
                {
                    case SettingKind.Toggle:
                        var box = values.GetBool(setting.Id) ? menuSkin.CheckBoxToggled : menuSkin.CheckBoxUntoggled;
                        renderer.Draw(box, controlRight - SettingsLayout.CheckBoxSize / 2f, centreY, SettingsLayout.CheckBoxSize, SettingsLayout.CheckBoxSize);
                        break;

                    case SettingKind.Dropdown:
                        // To be written

                    case SettingKind.NumberField:
                        float fieldCentreX = controlLeft + SettingsLayout.ControlWidth / 2f;
                        bool editing = setting.Id == editingId;

                        if (editing)   // outline while typing
                        {
                            renderer.DrawQuad(fieldCentreX, centreY, SettingsLayout.ControlWidth + 4, SettingsLayout.ControlHeight + 4, editBorderColour);
                        }

                        renderer.DrawQuad(fieldCentreX, centreY, SettingsLayout.ControlWidth, SettingsLayout.ControlHeight, fieldColour);

                        string text = editing ? editText + "_" : values.GetText(setting.Id);

                        if (setting.Kind == SettingKind.NumberField && setting.Suffix != null)
                        {
                            text += " " + setting.Suffix;
                        }

                        renderer.DrawText(glyphs, text, controlLeft + 14, textY, valueColour);

                        if (setting.Kind == SettingKind.Dropdown)
                        {
                            renderer.DrawText(glyphs, "v", controlRight - 28, textY, secondaryTextColour);
                        }

                        break;

                    case SettingKind.Submenu:
                        float arrowWidth = glyphs.MeasureText(">");
                        renderer.DrawText(glyphs, ">", controlRight - arrowWidth - 8, textY, valueColour);
                        break;
                }
            }
        }
    }
}