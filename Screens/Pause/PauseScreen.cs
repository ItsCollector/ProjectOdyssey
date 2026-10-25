using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.App;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.Screens.Components;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens.Pause
{
    public class PauseScreen : IGameScreen
    {
        private int windowWidth = 1920, windowHeight = 1080;

        private PauseView pauseScreenView;
        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }

        private Vector2 lastMousePos;
        private int HoveredButton = -1;

        public List<(int Slot, CardRect Rect)> ButtonRects { get; } = new()
        {
            (0, CardRect.FromCenter(960, 340, 300, 150)), // Continue button
            (1, CardRect.FromCenter(960, 540, 300, 150)), // Retry button
            (2, CardRect.FromCenter(960, 740, 300, 150))  // Exit button
        };

        public bool DrawsScreenBeneath => true;

        public PauseScreen(Renderer renderer, ScreenManager manager, AudioManager audioManager, SkinManager skinManager)
        {
            Renderer = renderer;
            ScreenManager = manager;
            AudioManager = audioManager;
            SkinManager = skinManager;
            pauseScreenView = new PauseView(SkinManager.MenuSkin, Renderer);
        }

        public void Render()
        {
            pauseScreenView.DrawPauseScreen();
        }

        public void Resize(int width, int height)
        {
            windowWidth = width;
            windowHeight = height;
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

            if (HoveredButton == 0) // Continue chart by unpausing
            {
                ScreenManager.Pop(popAction: PopAction.Resume);
                return;
            }
            else if (HoveredButton == 1) // Retry chart
            {
                ScreenManager.Pop(popAction: PopAction.Restart);
                return;
            }
            else if (HoveredButton == 2) // Exit to chart browser
            {
                ScreenManager.Pop(2);
                return;
            }
        }

        public void OnMouseMove(MouseMoveEventArgs e)
        {
            lastMousePos = e.Position;
            UpdateHover(ToLogical(lastMousePos));
        }

        // LogicalPos must already be in the fixed 1920x1080 space.
        public void UpdateHover(Vector2 logicalPos)
        {
            int hovered = -1;

            for (int i = 0; i < ButtonRects.Count; i++)
            {
                if (ButtonRects[i].Rect.Contains(logicalPos.X, logicalPos.Y))
                {
                    hovered = ButtonRects[i].Slot;
                    break;
                }
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

        public void Load() { }
        public void Update(float deltaMs) { }
        public void Unload() { }
    }
}
