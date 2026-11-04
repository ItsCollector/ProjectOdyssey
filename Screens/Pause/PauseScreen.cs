using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.App;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Common;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.Screens.Components;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens.Pause
{
    public class PauseScreen : IGameScreen
    {
        private Vector2 viewportDimensions;

        private PauseView pauseScreenView;
        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }
        private readonly List<Button> buttons;

        public bool DrawsScreenBeneath => true;

        public PauseScreen(Renderer renderer, ScreenManager manager, AudioManager audioManager, SkinManager skinManager)
        {
            Renderer = renderer;
            ScreenManager = manager;
            AudioManager = audioManager;
            SkinManager = skinManager;
            pauseScreenView = new PauseView(SkinManager.MenuSkin, Renderer);

            buttons = new List<Button>
            {
                new Button(960, 340, 300, 150, Unpause), // Continue button
                new Button(960, 540, 300, 150, Retry),   // Retry button
                new Button(960, 740, 300, 150, Exit)     // Exit button
            };
        }

        public void Render()
        {
            pauseScreenView.DrawPauseScreen();
        }

        public void Resize(Vector2 viewportDimensions)
        {
            this.viewportDimensions = viewportDimensions;
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

        private void Unpause()
        {
            ScreenManager.Pop(popAction: PopAction.Resume);
        }

        private void Retry()
        {
            ScreenManager.Pop(popAction: PopAction.Restart);
        }

        private void Exit()
        {
            ScreenManager.Pop(2);
        }

        public void Load() { }
        public void Update(float deltaMs) { }
        public void Unload() { }
    }
}
