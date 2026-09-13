namespace ProjectOdyssey.Screens
{
    public interface IGameScreen
    {
        // Called once when the ScreenManager pushes this screen. Do expensive
        // setup here (loading skins, starting sessions, etc.) rather than in
        // the constructor, so screens can be constructed cheaply ahead of time.
        void Load();

        // Called every frame by the ScreenManager before Render(), with the
        // elapsed time since the last frame in milliseconds.
        void Update(float deltaMs);

        void Render();

        // Called on window resize / framebuffer resize, and once immediately
        // after Load() so the screen starts with a correct viewport.
        void Resize(int width, int height);

        // Called once when the ScreenManager pops this screen. Release any
        // resources acquired in Load() here.
        void Unload();
    }
}
