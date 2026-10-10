using ProjectOdyssey.Engine;
using ProjectOdyssey.Render;
using System.Diagnostics.CodeAnalysis;

namespace ProjectOdyssey.Skinning
{
    // Loads every skin resource ONCE, up front, falling back to the default asset for
    // anything the skin is missing or that fails to load. After construction nothing
    // here touches the disk, so deleting an image mid-game can't break a screen.
    //
    // Must be constructed and disposed with the GL context current.
    public sealed class SkinManager : IDisposable
    {
        public const int DefaultGlyphSize = 40;
        private const string SkinFontFileName = "font.ttf";   // optional skin override

        private static readonly string AssetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        private static readonly string DefaultFontPath = Path.Combine(AssetsDir, "Fonts", "Exo2.ttf");
        private static readonly string DefaultChartBrowserDir = Path.Combine(AssetsDir, "Menus");
        private static readonly string DefaultGameplayDir = Path.Combine(AssetsDir, "Gameplay");
        private static readonly string DefaultJudgementDir = Path.Combine(AssetsDir, "Judgements");

        private static readonly (JudgementType Type, string File)[] JudgementFiles =
        {
            (JudgementType.Marvellous, "judge-marv.png"),
            (JudgementType.Perfect,    "judge-perfect.png"),
            (JudgementType.Great,      "judge-great.png"),
            (JudgementType.Good,       "judge-good.png"),
            (JudgementType.Bad,        "judge-bad.png"),
            (JudgementType.Miss,       "judge-miss.png"),
        };

        private readonly string[] skinFiles;
        private readonly List<Texture> ownedTextures = new();
        private readonly Dictionary<int, GlyphSet> ownedGlyphSets = new();
        private readonly List<string> defaultedAssets = new();
        private string? customFontPath;
        private bool disposed;

        // Names of assets that fell back to the default (for logging / a future settings screen)
        public IReadOnlyList<string> DefaultedAssets => defaultedAssets;

        public MenuSkin MenuSkin { get; }
        public GameplaySkin Gameplay { get; }
        public HudSkin Hud { get; }

        // skinDirectory = null means "use all defaults"
        public SkinManager(string? skinDirectory)
        {
            skinFiles = ListSkinFiles(skinDirectory);
            customFontPath = FindSkinFile(SkinFontFileName);

            try
            {
                // One font for menus and gameplay. Sizes are loaded here, eagerly, so no
                // renderer ever needs to open the font file at runtime.
                GlyphSet glyphs = GetGlyphs(DefaultGlyphSize);

                MenuSkin = new MenuSkin
                {
                    MissingBackground = LoadTexture("missing_background_image.png", DefaultChartBrowserDir),
                    SetCard = LoadTexture("set_card.png", DefaultChartBrowserDir),
                    SetCardHover = LoadTexture("set_card_hover.png", DefaultChartBrowserDir),
                    ChartCard = LoadTexture("chart_card.png", DefaultChartBrowserDir),
                    ChartCardHover = LoadTexture("chart_card_hover.png", DefaultChartBrowserDir),
                    PausedBackground = LoadTexture("paused_background.png", DefaultChartBrowserDir),
                    Glyphs = glyphs
                };

                Gameplay = new GameplaySkin
                {
                    Config = LoadGameplayConfig(),
                    TapNotes = LoadVariants("tap_note"),
                    LnHeads = LoadVariants("ln_head"),
                    LnBody = LoadTexture("ln_body.png", DefaultGameplayDir),
                    LnTail = LoadTexture("ln_tail.png", DefaultGameplayDir),
                    JudgementLine = LoadTexture("judgement_line.png", DefaultGameplayDir),
                    ReceptorUp = LoadTexture("receptor_up.png", DefaultGameplayDir),
                    ReceptorDown = LoadTexture("receptor_down.png", DefaultGameplayDir)
                };

                var judgements = new Dictionary<JudgementType, Texture>();
                foreach (var (type, file) in JudgementFiles)
                    judgements[type] = LoadTexture(file, DefaultJudgementDir);

                Hud = new HudSkin { Judgements = judgements, Glyphs = glyphs };

                if (defaultedAssets.Count > 0)
                    Console.WriteLine($"[INFO] Skin fell back to defaults for: {string.Join(", ", defaultedAssets)}");
            }
            catch
            {
                // A default asset is missing/corrupt: fail loudly, but don't leak what already loaded.
                Dispose();
                throw;
            }
        }

        // Loading helpers

        private static string[] ListSkinFiles(string? skinDirectory)
        {
            if (skinDirectory == null) return Array.Empty<string>();

            var result = GameplaySkinParser.GetFiles(skinDirectory);
            if (!result.IsSuccess)
            {
                Console.WriteLine($"[WARN] Could not read skin directory '{skinDirectory}', using defaults. {result.Error}");
                return Array.Empty<string>();
            }
            return result.Value;
        }

        // Case-insensitive lookup in the skin directory (matches how config.json was already found)
        private string? FindSkinFile(string fileName) =>
            skinFiles.FirstOrDefault(p => Path.GetFileName(p).Equals(fileName, StringComparison.OrdinalIgnoreCase));

        private Texture Own(Texture texture)
        {
            ownedTextures.Add(texture);
            return texture;
        }

        private static bool TryLoadTexture(string path, [NotNullWhen(true)] out Texture? texture)
        {
            try
            {
                texture = Texture.FromFile(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Bad skin file '{path}': {ex.Message}");
                texture = null;
                return false;
            }
        }

        // Skin file if present and valid, otherwise the default (which throws if it's missing).
        private Texture LoadTexture(string fileName, string defaultDirectory)
        {
            string? custom = FindSkinFile(fileName);
            if (custom != null && TryLoadTexture(custom, out Texture? skinTexture))
                return Own(skinTexture);

            defaultedAssets.Add(fileName);
            return Own(Texture.FromFile(Path.Combine(defaultDirectory, fileName)));
        }

        // "{baseName}_N.png" variants. Unloadable variants are skipped; if none survive,
        // the default skin's variants are used.
        private Texture[] LoadVariants(string baseName)
        {
            var found = GameplaySkinParser.FindImageVariants(skinFiles, baseName);
            if (found.IsSuccess)
            {
                var loaded = new List<Texture>();
                foreach (string path in found.Value)
                {
                    if (TryLoadTexture(path, out Texture? texture))
                        loaded.Add(Own(texture));
                }

                if (loaded.Count > 0) return loaded.ToArray();
            }

            defaultedAssets.Add($"{baseName}_*.png");

            var defaultFiles = GameplaySkinParser.GetFiles(DefaultGameplayDir);
            if (!defaultFiles.IsSuccess)
                throw new InvalidOperationException($"Default gameplay assets unreadable: {defaultFiles.Error}");

            var defaults = GameplaySkinParser.FindImageVariants(defaultFiles.Value, baseName);
            if (!defaults.IsSuccess)
                throw new InvalidOperationException($"Default gameplay assets missing: {defaults.Error}");

            return defaults.Value.Select(p => Own(Texture.FromFile(p))).ToArray();
        }

        private GameplaySkinConfig LoadGameplayConfig()
        {
            var result = GameplaySkinParser.ParseSkinConfig(skinFiles);
            if (result.IsSuccess) return result.Value;

            Console.WriteLine($"[WARN] {result.Error} Using default gameplay config.");
            defaultedAssets.Add("config.json");
            return new GameplaySkinConfig();
        }

        // Skin font if it has one and it loads, otherwise Exo2. Loaded once per size; the
        // returned set is owned by this manager.
        private GlyphSet GetGlyphs(int size)
        {
            if (ownedGlyphSets.TryGetValue(size, out GlyphSet? cached)) return cached;

            GlyphSet? set = null;

            if (customFontPath != null)
            {
                try
                {
                    set = GlyphSet.Load(customFontPath, size);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] Bad skin font '{customFontPath}': {ex.Message}");
                    customFontPath = null;   // stay on the default font for any later sizes too
                    defaultedAssets.Add(SkinFontFileName);
                }
            }

            set ??= GlyphSet.Load(DefaultFontPath, size);   // throws if the default is missing

            ownedGlyphSets[size] = set;
            return set;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            foreach (var set in ownedGlyphSets.Values) set.Dispose();
            ownedGlyphSets.Clear();

            foreach (var texture in ownedTextures) texture.Dispose();
            ownedTextures.Clear();
        }
    }
}
