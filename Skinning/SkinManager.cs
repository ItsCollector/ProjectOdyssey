using ProjectOdyssey.Engine;
using ProjectOdyssey.Render;
using System.Diagnostics;

namespace ProjectOdyssey.Skinning
{
    // Loads every skin resource ONCE, up front. The default skin is always loaded first so every
    // key count has a gameplay skin; the chosen skin then overrides whichever key counts it defines.
    // After construction nothing here touches the disk.
    //
    // Must be constructed and disposed with the GL context current.
    public sealed class SkinManager : IDisposable
    {
        public const int DefaultGlyphSize = 40;
        private const byte MinKeyCount = 4;
        private const byte MaxKeyCount = 10;

        private static readonly string DefaultSkinDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "DefaultSkin");
        private static readonly string DefaultFontPath = Path.Combine(DefaultSkinDirectory, "Fonts", "Exo2.ttf");
        private static readonly string DefaultMenuPath = Path.Combine(DefaultSkinDirectory, "Menus");

        private readonly Dictionary<int, GlyphSet> ownedGlyphSets = new();
        private readonly Dictionary<string, Texture> textureCache = new(StringComparer.OrdinalIgnoreCase); // owns every texture
        private bool disposed;

        public MenuSkin MenuSkin { get; }
        public Dictionary<byte, GameplaySkin> GameplaySkins { get; } = new();
        public Dictionary<byte, HudSkin> HudSkins { get; } = new();

        // skinDirectory = null means "use all defaults"
        public SkinManager(string? skinDirectory)
        {
            try
            {
                // One font for menus and gameplay. Sizes are loaded here, eagerly, so no
                // renderer ever needs to open the font file at runtime.
                GlyphSet glyphs = GetGlyphs(DefaultGlyphSize);

                MenuSkin = new MenuSkin
                {
                    MissingBackground = LoadTexture(Path.Combine(DefaultMenuPath, "missing_background_image.png")),
                    SetCard = LoadTexture(Path.Combine(DefaultMenuPath, "set_card.png")),
                    SetCardHover = LoadTexture(Path.Combine(DefaultMenuPath, "set_card_hover.png")),
                    ChartCard = LoadTexture(Path.Combine(DefaultMenuPath, "chart_card.png")),
                    ChartCardHover = LoadTexture(Path.Combine(DefaultMenuPath, "chart_card_hover.png")),
                    PausedBackground = LoadTexture(Path.Combine(DefaultMenuPath, "paused_background.png")),
                    Glyphs = glyphs
                };

                // Default skin first: it must load, and it must define every key count
                var defaultResult = GameplaySkinParser.ParseSkinConfig(DefaultSkinDirectory);

                if (!defaultResult.IsSuccess)
                {
                    throw new InvalidOperationException($"Default skin config is unreadable: {defaultResult.Error}");
                }

                LoadSkin(defaultResult.Value, DefaultSkinDirectory, required: true);
                EnsureAllKeyCountsLoaded();

                // Chosen skin second: overrides the key counts it defines, anything broken keeps the default
                if (skinDirectory != null)
                {
                    var result = GameplaySkinParser.ParseSkinConfig(skinDirectory);

                    if (result.IsSuccess)
                    {
                        LoadSkin(result.Value, skinDirectory, required: false);
                    }
                    else
                    {
                        Console.WriteLine($"[WARN] {result.Error} Using the default skin.");
                    }
                }
            }
            catch
            {
                // A default asset is missing/corrupt: fail loudly, but don't leak what already loaded.
                Dispose();
                throw;
            }
        }

        // required = true (default skin): any failure is fatal.
        // required = false (chosen skin): a key count that fails to load is skipped and keeps the default.
        private void LoadSkin(Skin skin, string directory, bool required)
        {
            foreach (GameplaySkinConfiguration config in skin.GameplaySkins)
            {
                try
                {
                    // Build both before storing either, so a failure can't leave a key count half-replaced
                    GameplaySkin gameplaySkin = BuildGameplaySkin(config, directory);
                    HudSkin hudSkin = BuildHudSkin(config, directory);

                    GameplaySkins[config.KeyCount] = gameplaySkin;
                    HudSkins[config.KeyCount] = hudSkin;
                }
                catch (Exception ex) when (!required)
                {
                    Console.WriteLine($"[WARN] {config.KeyCount}K skin in '{directory}' failed to load, keeping the default: {ex.Message}");
                }
            }
        }

        // The game looks skins up by the chart's key count, so a gap in the default skin would only show up mid-game
        private void EnsureAllKeyCountsLoaded()
        {
            var missing = new List<byte>();

            for (byte keyCount = MinKeyCount; keyCount <= MaxKeyCount; keyCount++)
            {
                if (!GameplaySkins.ContainsKey(keyCount))
                {
                    missing.Add(keyCount);
                }
            }

            if (missing.Count > 0)
            {
                throw new InvalidOperationException($"Default skin config has no entry for: {string.Join("K, ", missing)}K.");
            }
        }

        // Only the images the target type actually uses are required
        private GameplaySkin BuildGameplaySkin(GameplaySkinConfiguration config, string directory)
        {
            Texture? receptorUp = null;
            Texture? receptorDown = null;
            Texture? judgementLine = null;

            if (config.TargetType == TargetType.Receptor)
            {
                receptorUp = LoadRequiredTexture(directory, config.KeyUpReceptorImage, "KeyUpReceptorImage");
                receptorDown = LoadRequiredTexture(directory, config.KeyDownReceptorImage, "KeyDownReceptorImage");
            }
            else
            {
                judgementLine = LoadRequiredTexture(directory, config.JudgementLineImage, "JudgementLineImage");
            }

            return new GameplaySkin
            {
                KeyCount = config.KeyCount,
                NoteWidth = config.NoteWidth,
                NoteHeight = config.NoteHeight,
                HitPositionX = config.HitPositionX,
                HitPositionY = config.HitPositionY,
                ColumnSpacing = config.ColumnSpacing,
                TargetType = config.TargetType,
                TapNotes = LoadColumnTextures(directory, config.TapNoteImage, config.KeyCount, "TapNoteImage"),
                LnHeads = LoadColumnTextures(directory, config.LnHeadImage, config.KeyCount, "LnHeadImage"),
                LnBodies = LoadColumnTextures(directory, config.LnBodyImage, config.KeyCount, "LnBodyImage"),
                LnTails = LoadColumnTextures(directory, config.LnTailImage, config.KeyCount, "LnTailImage"),
                ReceptorUp = receptorUp,
                ReceptorDown = receptorDown,
                JudgementLine = judgementLine
            };
        }

        private HudSkin BuildHudSkin(GameplaySkinConfiguration config, string directory)
        {
            var judgements = new Dictionary<JudgementType, Texture>
            {
                [JudgementType.Marvellous] = LoadRequiredTexture(directory, config.JudgementMarvellousImage, "JudgementMarvellousImage"),
                [JudgementType.Perfect] = LoadRequiredTexture(directory, config.JudgementPerfectImage, "JudgementPerfectImage"),
                [JudgementType.Great] = LoadRequiredTexture(directory, config.JudgementGreatImage, "JudgementGreatImage"),
                [JudgementType.Good] = LoadRequiredTexture(directory, config.JudgementGoodImage, "JudgementGoodImage"),
                [JudgementType.Bad] = LoadRequiredTexture(directory, config.JudgementBadImage, "JudgementBadImage"),
                [JudgementType.Miss] = LoadRequiredTexture(directory, config.JudgementMissImage, "JudgementMissImage"),
            };

            return new HudSkin { Judgements = judgements, Glyphs = GetGlyphs(DefaultGlyphSize) };
        }

        // One texture per column, so the view can index by column. A list shorter than the key count
        // cycles, which means a single entry applies to every column.
        private Texture[] LoadColumnTextures(string directory, List<string>? imagePaths, byte keyCount, string label)
        {
            if (imagePaths == null || imagePaths.Count == 0)
                throw new InvalidOperationException($"{label} has no images.");

            Texture[] loaded = imagePaths.Select(path => LoadTexture(Path.Combine(directory, path))).ToArray();

            var perColumn = new Texture[keyCount];
            for (int column = 0; column < keyCount; column++)
                perColumn[column] = loaded[column % loaded.Length];

            return perColumn;
        }

        private Texture LoadRequiredTexture(string directory, string? relativePath, string label)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new InvalidOperationException($"{label} is missing.");

            return LoadTexture(Path.Combine(directory, relativePath));
        }

        // Loads a texture once per file. The cache owns it, so shared images only load onto the GPU once.
        // Accepts either \ or / in paths, so a config.json written on Windows still works elsewhere.
        private Texture LoadTexture(string path)
        {
            string fullPath = Path.GetFullPath(path.Replace('\\', Path.DirectorySeparatorChar));

            if (textureCache.TryGetValue(fullPath, out Texture? cached)) return cached;

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Texture file not found: {fullPath}", fullPath);

            Texture texture = Texture.FromFile(fullPath);
            textureCache[fullPath] = texture;
            return texture;
        }

        // Loaded once per size; the returned set is owned by this manager.
        private GlyphSet GetGlyphs(int size)
        {
            if (ownedGlyphSets.TryGetValue(size, out GlyphSet? cached)) return cached;

            var set = GlyphSet.Load(DefaultFontPath, size);
            ownedGlyphSets[size] = set;

            return set;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            foreach (var set in ownedGlyphSets.Values) set.Dispose();
            ownedGlyphSets.Clear();

            foreach (var texture in textureCache.Values) texture.Dispose();
            textureCache.Clear();
        }
    }
}