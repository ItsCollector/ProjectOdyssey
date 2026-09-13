namespace ProjectOdyssey.Screens
{
    // Owns the stack of active screens and drives their lifecycle. MainWindow
    // should only ever talk to this class, never to a concrete IGameScreen -
    // that's what let MainWindow shrink down to just forwarding GameWindow
    // events instead of knowing about GameplayScreen/ChartManagerScreen etc.
    public class ScreenManager
    {
        private readonly Stack<IGameScreen> screens = new();
        private int viewportWidth;
        private int viewportHeight;

        public bool HasScreen => screens.Count > 0;

        // Pushes a new screen on top of the stack (e.g. gameplay -> pause menu).
        // The screen underneath is left loaded but no longer updated/rendered
        // until this one is popped.
        public void Push(IGameScreen screen)
        {
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
            if (screens.Count > 0)
                screens.Peek().Render();
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
    }
}
