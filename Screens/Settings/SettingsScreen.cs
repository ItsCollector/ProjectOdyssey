using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.App;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Common;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.Screens.Components;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens.Settings
{
    public class SettingsScreen : IGameScreen, ISettingsValues
    {
        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }
        public bool DrawsScreenBeneath => true;

        private readonly SettingsView settingsView;
        private readonly GameSettings settings = GameSettings.Current;
        private readonly List<SettingsEntry> entries = SettingsLayout.BuildEntries();
        private readonly StackView settingsStackView = new StackView(SettingsLayout.FirstRowCenterY);

        private Vector2 viewportDimensions;
        private Vector2 lastMousePos;
        private float openAmount; // 0 = hidden, 1 = open
        private bool closing;
        private StackItem? hovered;
        private string? editingId; // number field being typed into
        private string editText = "";

        // ISettingsValues: the view reads values through these
        public bool GetBool(string id) => settings.GetBool(id);
        public string GetText(string id) => settings.GetText(id);
        public bool IsDisabled(SettingDefinition s) => s.DisabledWhenOn != null && settings.GetBool(s.DisabledWhenOn);

        public SettingsScreen(Renderer renderer, ScreenManager screenManager, AudioManager audioManager, SkinManager skinManager)
        {
            Renderer = renderer;
            ScreenManager = screenManager;
            AudioManager = audioManager;
            SkinManager = skinManager;
            settingsView = new SettingsView(SkinManager.MenuSkin, Renderer);
            ConstructSettingStack();
        }

        public void Load() { }

        public void Unload()
        {
            CommitEdit();
            settings.Save();
        }

        public void Resize(Vector2 viewportDimensions) => this.viewportDimensions = viewportDimensions;

        private void ConstructSettingStack()
        {
            settingsStackView.Begin();
            for (int i = 0; i < entries.Count; i++)
            {
                bool header = entries[i].IsHeader;
                settingsStackView.Add(i, SettingsLayout.RowX, SettingsLayout.RowWidth,
                    header ? SettingsLayout.HeaderHeight : SettingsLayout.RowHeight,
                    header ? SettingsLayout.HeaderSpacing : SettingsLayout.RowSpacing,
                    tag: i, kind: header ? SettingsLayout.HeaderKind : SettingsLayout.RowKind);
            }
            settingsStackView.End(0);
        }

        public void Update(float deltaMs)
        {
            // Ease the panel open (or closed, then pop once it has slid away)
            float target = closing ? 0f : 1f;
            openAmount += (target - openAmount) * (1f - MathF.Exp(-16f * deltaMs / 1000f));
            if (MathF.Abs(target - openAmount) < 0.002f) openAmount = target;
            if (closing && openAmount == 0f) { ScreenManager.Pop(); return; }

            settingsStackView.Update(deltaMs);
            settingsStackView.SnapBack(deltaMs);

            var item = FindItemAt(ToLogical.ViewportToLogical(lastMousePos, viewportDimensions));
            hovered = item != null && !IsDisabled(entries[item.Tag].Setting!) ? item : null;
        }

        public void Render()
        {
            settingsView.DrawSettingsScreen(settingsStackView.Items, entries, this, openAmount, hovered, editingId, editText);
        }

        // Stack X values are the final positions, so shift the mouse by the panel's slide offset instead.
        private StackItem? FindItemAt(Vector2 logical)
        {
            float x = logical.X - SettingsLayout.PanelX(openAmount);
            foreach (var item in settingsStackView.Items)
            {
                if (item.Kind == SettingsLayout.HeaderKind) continue;
                if (item.Contains(x, logical.Y)) return item;
            }
            return null;
        }

        private SettingDefinition EditingDefinition => entries.First(e => e.Setting?.Id == editingId).Setting!;

        private void CommitEdit()
        {
            if (editingId == null) return;
            var def = EditingDefinition;
            if (int.TryParse(editText, out int value))
                settings.SetNumber(def.Id, (int)Math.Clamp(value, def.Min, def.Max));
            editingId = null;
        }

        public void OnMouseMove(MouseMoveEventArgs e) => lastMousePos = e.Position;

        public void OnMouseWheel(MouseWheelEventArgs e) => settingsStackView.Scroll(e.OffsetY * 60f);

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button != MouseButton.Left) return;

            CommitEdit(); // clicking anywhere finishes a field being edited

            var item = FindItemAt(ToLogical.ViewportToLogical(lastMousePos, viewportDimensions));
            if (item == null) return;

            var def = entries[item.Tag].Setting!;
            if (IsDisabled(def)) return;

            switch (def.Kind)
            {
                case SettingKind.Toggle: settings.SetBool(def.Id, !settings.GetBool(def.Id)); break;
                case SettingKind.Dropdown: settings.CycleDropdown(def.Id); break;
                case SettingKind.NumberField: editingId = def.Id; editText = settings.GetText(def.Id); break;
                case SettingKind.Submenu: /* TODO: open the bindings panel */ break;
            }
        }

        public void OnKeyDown(Keys key, KeyModifiers modifiers)
        {
            if (editingId != null)
            {
                switch (key)
                {
                    case Keys.Enter:
                    case Keys.KeyPadEnter: CommitEdit(); break;
                    case Keys.Escape: editingId = null; break;
                    case Keys.Backspace: if (editText.Length > 0) editText = editText[..^1]; break;
                }
                return;   // while typing, keys are not shortcuts
            }

            if (key == Keys.Escape || (key == Keys.O && modifiers.HasFlag(KeyModifiers.Control)))
                closing = true;
        }

        public void OnTextInput(TextInputEventArgs e)
        {
            if (editingId == null) return;
            var def = EditingDefinition;

            foreach (char c in e.AsString)
            {
                bool digit = char.IsDigit(c);
                bool minus = c == '-' && def.Min < 0 && editText.Length == 0;
                if ((digit || minus) && editText.Length < 6) editText += c;
            }
        }
    }
}