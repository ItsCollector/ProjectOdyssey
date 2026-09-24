using System.Text.Json;
using ProjectOdyssey.Common;

namespace ProjectOdyssey.Skinning
{
    public static class GameplaySkinParser
    {
        public static Result<(GameplaySkinConfig, SkinAssets)> LoadSkin()
        {
            string skinDirectory = Path.Combine(AppContext.BaseDirectory, "skins/Skin 1");

            // Get the skin files from the skin directory
            var filesResult = GetFiles(skinDirectory);
            if (!filesResult.IsSuccess)
            {
                return Result<(GameplaySkinConfig, SkinAssets)>.Err(filesResult.Error);
            }

            // Load the skin config 
            var configResult = ParseSkinConfig(filesResult.Value);
            if (!configResult.IsSuccess)
            {
                return Result<(GameplaySkinConfig, SkinAssets)>.Err(configResult.Error);
            }

            // Load skin assets
            var assetsResult = DiscoverAssets(filesResult.Value);
            if (!assetsResult.IsSuccess)
            {
                return Result<(GameplaySkinConfig, SkinAssets)>.Err(assetsResult.Error);
            }

            return Result<(GameplaySkinConfig, SkinAssets)>.Ok((configResult.Value, assetsResult.Value));
        }

        public static Result<string[]> GetFiles(string skinDirectory)
        {
            try
            {
                return Result<string[]>.Ok(Directory.GetFiles(skinDirectory));
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

        // Discovers every required skin component and returns them as one bundle,
        // or the first missing-component error encountered.
        public static Result<SkinAssets> DiscoverAssets(string[] files)
        {
            var tapNotes = FindImageVariants(files, "tap_note");
            if (!tapNotes.IsSuccess) return Result<SkinAssets>.Err(tapNotes.Error);

            var lnHeads = FindImageVariants(files, "ln_head");
            if (!lnHeads.IsSuccess) return Result<SkinAssets>.Err(lnHeads.Error);

            var lnBody = FindImage(files, "ln_body");
            if (!lnBody.IsSuccess) return Result<SkinAssets>.Err(lnBody.Error);

            var lnTail = FindImage(files, "ln_tail");
            if (!lnTail.IsSuccess) return Result<SkinAssets>.Err(lnTail.Error);

            var judgementLine = FindImage(files, "judgement_line");
            if (!judgementLine.IsSuccess) return Result<SkinAssets>.Err(judgementLine.Error);

            var receptorUp = FindImage(files, "receptor_up");
            if (!receptorUp.IsSuccess) return Result<SkinAssets>.Err(receptorUp.Error);

            var receptorDown = FindImage(files, "receptor_down");
            if (!receptorDown.IsSuccess) return Result<SkinAssets>.Err(receptorDown.Error);

            return Result<SkinAssets>.Ok(new SkinAssets
            {
                TapNotePaths = tapNotes.Value,
                LnHeadPaths = lnHeads.Value,
                LnBodyPath = lnBody.Value,
                LnTailPath = lnTail.Value,
                JudgementLinePath = judgementLine.Value,
                ReceptorUpPath = receptorUp.Value,
                ReceptorDownPath = receptorDown.Value
            });
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

        // Finds a single required file, "{name}.png".
        public static Result<string> FindImage(string[] files, string name)
        {
            foreach (string filePath in files)
            {
                if (Path.GetFileName(filePath).Equals($"{name}.png", StringComparison.OrdinalIgnoreCase))
                    return Result<string>.Ok(filePath);
            }

            return Result<string>.Err($"Missing required image '{name}.png'");
        }

        private static int? ParseTrailingNumber(string fileName, string baseName)
        {
            string prefix = baseName + "_";
            if (!fileName.StartsWith(prefix)) return null;

            string suffix = fileName.Substring(prefix.Length);
            return int.TryParse(suffix, out int n) ? n : null;
        }
    }
}