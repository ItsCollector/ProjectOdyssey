using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Render;
using ProjectOdyssey.Skinning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Screens
{
    public class PauseScreen : IGameScreen
    {
        private PauseScreenRenderer pauseScreenRenderer;

        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }

        public bool DrawsScreenBeneath => true;

        public void Load()
        {
            pauseScreenRenderer = new PauseScreenRenderer(SkinManager.MenuSkin);
            pauseScreenRenderer.Initialise();
        }

        public void Update(float deltaMs)
        {
            // Update pause screen logic here
        }

        public void Render()
        {
            pauseScreenRenderer.DrawPauseScreen();
        }

        public void Resize(int width, int height)
        {
            pauseScreenRenderer.Resize(width, height);
        }

        public void Unload()
        {
            pauseScreenRenderer.Dispose();
        }

        public void OnKeyDown(Keys key)
        {
            if (key == Keys.Escape)
            {
                ScreenManager.Pop();
            }
        }
    }
}
