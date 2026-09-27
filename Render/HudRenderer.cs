using System.Runtime.CompilerServices;
using OpenTK.Mathematics;

namespace ProjectOdyssey.Render
{
    public class HudRenderer : Renderer
    {
        private FontRenderer fontRenderer = new FontRenderer();
        private Dictionary<char, FreeTypeGlyph> glyphs_40;
        private Vector4 primaryTextColour = new Vector4(1f, 1f, 1f, 1f);

        private List<Texture> judgementTextures = new List<Texture>();

        public HudRenderer()
        {
            glyphs_40 = fontRenderer.LoadGlyphs(40);
        }

        public void Initialise()
        {
            fontRenderer.Intitialise();
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            fontRenderer.Resize(1920, 1080);
        }

        public void DrawHud(int combo)
        {
            if (combo > 0)
            {
                fontRenderer.Draw(glyphs_40, combo.ToString(), 960, 540, primaryTextColour);
            }
        }
    }
}
