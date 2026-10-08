using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens
{
    // Owns the stack of active screens and drives their lifecycle. MainWindow
    // should only ever talk to this class, never to a concrete IGameScreen -
    // that's what let MainWindow shrink down to just forwarding GameWindow
    // events instead of knowing about GameplayScreen/ChartManagerScreen etc.
    public class ScreenManager : IDisposable
    {
        private readonly AudioManager audioManager;
        private SkinManager? skinManager;  
        private readonly Stack<IGameScreen> screens = new();
        private readonly List<IGameScreen> renderScratch = new(); 
        private int viewportWidth;
        private int viewportHeight;

        public bool HasScreen => screens.Count > 0;

        public ScreenManager()
        {
            this.audioManager = new AudioManager();
        }   

        // Loads the skin (textures + glyphs). Call once from MainWindow.OnLoad, after the
        // GL context exists and before the first Push. Constructing this earlier (e.g. in a
        // field initialiser) runs GL calls with no context.
        public void Initialise(string? skinDirectory)
        {
            if (skinManager != null)
            {
                throw new InvalidOperationException("ScreenManager is already initialised; live screens hold references to the skin's textures.");
            }

            skinManager = new SkinManager(skinDirectory);
        }

        // Pushes a new screen on top of the stack (e.g. gameplay -> pause menu).
        // The screen underneath is left loaded but no longer updated/rendered
        // until this one is popped.
        public void Push(IGameScreen screen)
        {
            if (skinManager == null)
                throw new InvalidOperationException("Call Initialise() before pushing screens.");

            screen.ScreenManager = this;
            screen.AudioManager = audioManager;
            screen.SkinManager = skinManager;
            screen.Load();
            screen.Resize(viewportWidth, viewportHeight);
            screens.Push(screen);
        }

        // Pops the current screen, returning control to whatever is beneath it.
        public void Pop()
        {
            if (screens.Count == 0) return;
            screens.Pop().Unload();
        }

        // Convenience for a straight swap (e.g. song select -> gameplay) where
        // you don't want the previous screen kept around underneath.
        public void Replace(IGameScreen screen)
        {
            Pop();
            Push(screen);
        }

        public void Update(float deltaMs)
        {
            if (screens.Count > 0)
                screens.Peek().Update(deltaMs);
        }

        public void Render()
        {
            renderScratch.Clear();

            // Stack<T> enumerates top -> bottom. Collect until we hit an opaque screen (inclusive).
            foreach (var screen in screens)
            {
                renderScratch.Add(screen);
                if (!screen.DrawsScreenBeneath) break;
            }

            // Draw bottom -> top so the pause menu lands over gameplay.
            for (int i = renderScratch.Count - 1; i >= 0; i--)
            {
                renderScratch[i].Render();
            }
        }

        // Resizes every screen on the stack, not just the top one, so a
        // screen underneath a pause overlay is still correctly sized if
        // it becomes visible again.
        public void Resize(int width, int height)
        {
            viewportWidth = width;
            viewportHeight = height;

            foreach (var screen in screens)
                screen.Resize(width, height);
        }

        public void UnloadAll()
        {
            while (screens.Count > 0)
                screens.Pop().Unload();
        }

        // Unloads every screen first (they borrow from the skin), then frees the skin and audio.
        // Call with the GL context still current.
        public void Dispose()
        {
            UnloadAll();
            skinManager?.Dispose();
            skinManager = null;
            audioManager.Dispose();
        }

        public void OnKeyDown(Keys key)
        {
            if (screens.Count > 0)
                screens.Peek().OnKeyDown(key);
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (screens.Count > 0)
                screens.Peek().OnMouseDown(e);
        }

        public void OnMouseMove(MouseMoveEventArgs e)
        {
            if (screens.Count > 0)
                screens.Peek().OnMouseMove(e);
        }

        public void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (screens.Count > 0)
                screens.Peek().OnMouseWheel(e);
        }
    }
}
