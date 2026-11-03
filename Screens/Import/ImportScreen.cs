using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.App;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Data.Charts;
using ProjectOdyssey.Input;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.Screens.ChartBrowser;
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

        private int windowWidth = 1920;
        private int windowHeight = 1080;

        private Vector2 lastMousePos;
        private int HoveredButton = -1;

        private CardRect importButton = CardRect.FromCenter(960, 215, 320, 90);

        private readonly List<string> rowsSnapshot = new();

        public ImportScreen(InputHistory inputHistory, Renderer renderer, ScreenManager screenManager, AudioManager audioManager, SkinManager skinManager)
        {
            InputHistory = inputHistory;
            Renderer = renderer;
            ScreenManager = screenManager;
            AudioManager = audioManager;
            SkinManager = skinManager;
            importView = new ImportScreenView(skinManager.MenuSkin, Renderer);
        }

        public void Load()
        {
            
        }

        public void Render()
        {
            bool importing = ImportProgress.Snapshot(rowsSnapshot);
            importView.DrawImportScreen(rowsSnapshot, importing);
        }

        public void Unload()
        {
            
        }

        public void Update(float deltaMs)
        {
            
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
            if (e.Button != MouseButton.Left || HoveredButton == -1) return;

            if (HoveredButton == 0)
            {
                ImportOsuContent();
                return;
            }
        }

        public void OnMouseMove(MouseMoveEventArgs e)
        {
            lastMousePos = e.Position;
            UpdateHover(ToLogical(lastMousePos));
        }

        public void UpdateHover(Vector2 logicalPos)
        {
            int hovered = -1;

            if (importButton.Contains(logicalPos.X, logicalPos.Y))
            {
                hovered = 0;    
            }

            if (hovered != HoveredButton)
            {
                HoveredButton = hovered;
            }
        }

        private Vector2 ToLogical(Vector2 windowPos)
        {
            return new Vector2(windowPos.X * 1920f / windowWidth, windowPos.Y * 1080f / windowHeight);
        }

        public void ImportOsuContent()
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

        public void Resize(int width, int height)
        {
            windowWidth = width;
            windowHeight = height;
        }
    }
}
