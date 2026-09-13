using ProjectOdyssey.IO;

namespace ProjectOdyssey.Screens
{
    public class ChartManagerScreen : IGameScreen
    {
        public void Load()
        {
            ChartImportService.ImportChartsFromOsu();
        }

        public void Update(float deltaMs)
        {
            // Chart manager UI update logic goes here
        }

        public void Render()
        {
            // Render the chart manager UI here
        }

        public void Resize(int width, int height)
        {
            // Handle viewport size changes if necessary
        }

        public void Unload()
        {
            // Clean up resources if necessary
        }
    }
}
