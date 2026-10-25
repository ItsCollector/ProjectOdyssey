using ProjectOdyssey.Rendering;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens.Pause
{
    public class PauseView
    {
        // Renderer instance that this class will use to draw textures
        private Renderer renderer;
        private readonly MenuSkin menuSkin;

        public PauseView(MenuSkin menuSkin, Renderer renderer)
        {
            this.menuSkin = menuSkin;
            this.renderer = renderer;
        }

        public void DrawPauseScreen()
        {
            // Draw the pause screen background
            renderer.Draw(menuSkin.PausedBackground, 960, 540);

            // Draw buttons
            renderer.Draw(menuSkin.ContinueButton, 960, 340);
            renderer.Draw(menuSkin.RetryButton, 960, 540); 
            renderer.Draw(menuSkin.ExitButton, 960, 740);
        }
    }
}
