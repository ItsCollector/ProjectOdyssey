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

        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }

        public bool DrawsScreenBeneath => true;

        public void Load()
        {
            pauseScreenRenderer = new PauseScreenRenderer(SkinManager.MenuSkin, Renderer);
        }

        public void Update(float deltaMs)
        {
            // Update pause screen logic here
        }

        public void Render()
        {
            pauseScreenRenderer.DrawPauseScreen();
        }

        public void Unload()
        {
            // Cleanup resources if needed
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
