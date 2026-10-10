using ProjectOdyssey.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectOdyssey.Skinning
{
    // File discovery + config parsing only. Deciding what to do when something is
    // missing (fall back to defaults) is the SkinManager's job.
    public static class GameplaySkinParser
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static Result<Skin> ParseSkinConfig(string directory)
        {
            try
            {
                foreach (string filePath in Directory.GetFiles(directory))
                {
                    if (Path.GetFileName(filePath).Equals("config.json", StringComparison.OrdinalIgnoreCase))
                    {
                        string json = File.ReadAllText(filePath);
                        Skin? skin = JsonSerializer.Deserialize<Skin>(json, JsonOptions);

                        if (skin == null)
                        {
                            return Result<Skin>.Err("config.json was empty or invalid.");
                        }

                        return Result<Skin>.Ok(skin);
                    }
                }

                return Result<Skin>.Err("No config.json found in skin directory.");
            }
            catch (Exception ex)
            {
                return Result<Skin>.Err($"Error parsing skin config: {ex.Message}");
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
