using ProjectOdyssey.Skinning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Render
{
    public class PauseScreenRenderer : Renderer
    {
        private readonly MenuSkin menuSkin;

        public PauseScreenRenderer(MenuSkin menuSkin)
        {
            this.menuSkin = menuSkin;
        }

        public void Initialise()
        {
            base.Intitialise();
        }

        public void DrawPauseScreen()
        {
            // Draw the pause screen background
            Draw(menuSkin.PausedBackground, 960, 540);
            // Draw the pause menu options (e.g., Resume, Restart, Exit)
            // You can use the fontRenderer to draw text for the menu options
            // Example:
            // fontRenderer.DrawText("Resume", 960, 400, primaryTextColour);
            // fontRenderer.DrawText("Restart", 960, 500, primaryTextColour);
            // fontRenderer.DrawText("Exit", 960, 600, primaryTextColour);
        }

        public override void Resize(int width, int height)
        {
            base.Resize(1920, 1080);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
