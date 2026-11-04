using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.App;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Common;
using ProjectOdyssey.Data.Charts;
using ProjectOdyssey.Input;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.Screens.Components;
using ProjectOdyssey.Skinning;


namespace ProjectOdyssey.Screens.Import
{
    class ImportScreen : IGameScreen
    {
        public InputHistory InputHistory { private get; set; }
        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }
        private ImportScreenView importView;

        private Vector2 viewportDimensions;

        private readonly List<string> rowsSnapshot = new();

        private readonly List<Button> buttons = new List<Button>
        {
            new Button(960, 215, 320, 90, ImportOsuContent)
        };

        public ImportScreen(InputHistory inputHistory, Renderer renderer, ScreenManager screenManager, AudioManager audioManager, SkinManager skinManager)
        {
            InputHistory = inputHistory;
            Renderer = renderer;
            ScreenManager = screenManager;
            AudioManager = audioManager;
            SkinManager = skinManager;
            importView = new ImportScreenView(skinManager.MenuSkin, Renderer);
        }

        public void Render()
        {
            bool importing = ImportProgress.Snapshot(rowsSnapshot);
            importView.DrawImportScreen(rowsSnapshot, importing);
        }

        public void OnKeyDown(Keys key)
        {
            if (key == Keys.Escape)
            {
                ScreenManager.Pop();
            }
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button != MouseButton.Left) return;

            foreach (Button button in buttons)
            {
                if (button.TryClick()) return; 
            }
        }

        public void OnMouseMove(MouseMoveEventArgs e)
        {
            Vector2 logicalPos = ToLogical.ViewportToLogical(e.Position, viewportDimensions);

            foreach (Button button in buttons)
            {
                button.UpdateHoverState(logicalPos.X, logicalPos.Y);
            }
        }

        private static void ImportOsuContent()
        {
            if (!ImportProgress.TryBeginImport()) return;

            Task.Run(() =>
            {
                try 
                { 
                    ChartImportService.ImportChartsFromOsu(); 
                }
                finally 
                { 
                    ImportProgress.EndImport(); 
                }
            });
        }

        public void Resize(Vector2 viewportDimensions)
        {
            this.viewportDimensions = viewportDimensions;
        }

        public void Load() {}

        public void Unload() {}

        public void Update(float deltaMs) {}
    }
}
