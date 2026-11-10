using System.Text.Json;
using ProjectOdyssey.Screens.Settings;

namespace ProjectOdyssey.App
{
    public enum DisplayMode { Windowed, Fullscreen, Borderless }

    public class GameSettings
    {
        // Hard-coded for now; later fill this from the monitor's supported modes.
        public static readonly (int W, int H)[] Resolutions =
        { 
            (1280, 720), 
            (1600, 900), 
            (1920, 1080), 
            (2560, 1440), 
            (3840, 2160) 
        };

        public static GameSettings Current { get; } = Load();
        public DisplayMode DisplayMode { get; set; } = DisplayMode.Borderless;
        public int ResolutionIndex { get; set; } = 2;
        public bool VSync { get; set; } = true;
        public int MaxFps { get; set; } = 240;
        public int Volume { get; set; } = 80;
        public int AudioOffsetMs { get; set; } = 0;

        // MainWindow subscribes to this to apply window, FPS and volume changes.
        public event Action? Changed;

        public bool GetBool(string id) => id == SettingIds.VSync && VSync;

        public void SetBool(string id, bool value)
        {
            if (id == SettingIds.VSync) VSync = value;
            Changed?.Invoke();
        }

        public string GetText(string id) => id switch
        {
            SettingIds.DisplayMode => DisplayMode.ToString(),
            SettingIds.Resolution => $"{Resolutions[ResolutionIndex].W}x{Resolutions[ResolutionIndex].H}",
            SettingIds.MaxFps => MaxFps.ToString(),
            SettingIds.Volume => Volume.ToString(),
            SettingIds.AudioOffset => AudioOffsetMs.ToString(),
            _ => ""
        };

        // Stand-in for a real dropdown popup: each click moves to the next option.
        public void CycleDropdown(string id)
        {
            if (id == SettingIds.DisplayMode)
            {
                DisplayMode = (DisplayMode)(((int)DisplayMode + 1) % Enum.GetValues<DisplayMode>().Length);
            }
            else if (id == SettingIds.Resolution)
            {
                ResolutionIndex = (ResolutionIndex + 1) % Resolutions.Length;
            }

            Changed?.Invoke();
        }

        // The caller clamps to the setting's Min/Max before calling this.
        public void SetNumber(string id, int value)
        {
            switch (id)
            {
                case SettingIds.MaxFps: MaxFps = value; break;
                case SettingIds.Volume: Volume = value; break;
                case SettingIds.AudioOffset: AudioOffsetMs = value; break;
            }

            Changed?.Invoke();
        }

        private static string FilePath => Path.Combine(AppContext.BaseDirectory, "settings.json");

        private static GameSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(FilePath));
                    if (loaded != null)
                    {
                        loaded.ResolutionIndex = Math.Clamp(loaded.ResolutionIndex, 0, Resolutions.Length - 1);
                        return loaded;
                    }
                }
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"[WARN] Could not read settings: {ex.Message}"); 
            }

            return new GameSettings();
        }

        public void Save()
        {
            try 
            { 
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })); 
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"[WARN] Could not save settings: {ex.Message}"); 
            }
        }
    }
}