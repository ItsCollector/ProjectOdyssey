using ProjectOdyssey.Audio;
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
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }

        public void Load()
        {
            // Load pause screen resources here
        }
        public void Update(float deltaMs)
        {
            // Update pause screen logic here
        }
        public void Render()
        {
            // Render pause screen here
        }
        public void Resize(int width, int height)
        {
            // Handle resize for pause screen here
        }
        public void Unload()
        {
            // Unload pause screen resources here
        }
    }
}
