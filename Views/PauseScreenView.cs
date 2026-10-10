using ProjectOdyssey.Skinning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Render
{
    public class PauseScreenView
    {
        // Renderer instance that this class will use to draw textures
        private Renderer renderer;

        private readonly MenuSkin menuSkin;

        public PauseScreenView(MenuSkin menuSkin, Renderer renderer)
        {
            this.menuSkin = menuSkin;
            this.renderer = renderer;
        }

        public void DrawPauseScreen()
        {
            // Draw the pause screen background
            renderer.Draw(menuSkin.PausedBackground, 960, 540);
        }
    }
}
