using System.Text.Json;
using ProjectOdyssey.Common;

namespace ProjectOdyssey.Skinning
{
    // File discovery + config parsing only. Deciding what to do when something is
    // missing (fall back to defaults) is the SkinManager's job.
    public static class GameplaySkinParser
    {
        public static Result<string[]> GetFiles(string directory)
        {
            try
            {
                return Result<string[]>.Ok(Directory.GetFiles(directory));
            }
            catch (Exception ex)
            {
                return Result<string[]>.Err($"[Error] {ex.Message}");
            }
        }

        public static Result<GameplaySkinConfig> ParseSkinConfig(string[] files)
        {
            try
            {
                foreach (string filePath in files)
                {
                    if (Path.GetFileName(filePath).Equals("config.json", StringComparison.OrdinalIgnoreCase))
                    {
                        string json = File.ReadAllText(filePath);
                        GameplaySkinConfig? skin = JsonSerializer.Deserialize<GameplaySkinConfig>(json);

                        if (skin == null)
                            return Result<GameplaySkinConfig>.Err("config.json was empty or invalid.");

                        if (skin.NoteWidth <= 0 || skin.NoteHeight <= 0)
                            return Result<GameplaySkinConfig>.Err("config.json: NoteWidth and NoteHeight must be positive.");

                        return Result<GameplaySkinConfig>.Ok(skin);
                    }
                }

                return Result<GameplaySkinConfig>.Err("No config.json found in skin directory.");
            }
            catch (Exception ex)
            {
                return Result<GameplaySkinConfig>.Err($"Error parsing skin config: {ex.Message}");
            }
        }

        // Finds every "{baseName}_N.png" file, sorted by N. At least one must exist.
        public static Result<string[]> FindImageVariants(string[] files, string baseName)
        {
            var matches = files
                .Where(path => Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase))
                .Select(path => new
                {
                    Path = path,
                    Number = ParseTrailingNumber(Path.GetFileNameWithoutExtension(path), baseName)
                })
                .Where(x => x.Number != null)
                .OrderBy(x => x.Number)
                .Select(x => x.Path)
                .ToArray();

            if (matches.Length == 0)
                return Result<string[]>.Err($"No images found matching '{baseName}_*.png'");

            return Result<string[]>.Ok(matches);
        }

        private static int? ParseTrailingNumber(string fileName, string baseName)
        {
            string prefix = baseName + "_";
            if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;

            string suffix = fileName.Substring(prefix.Length);
            return int.TryParse(suffix, out int n) ? n : null;
        }
    }
}
