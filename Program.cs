using ProjectOdyssey.App;

namespace ProjectOdyssey
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var settings = GameSettings.Current;   // loads settings.json here, at start-up
            var (width, height) = GameSettings.Resolutions[settings.ResolutionIndex];

            using (MainWindow window = new MainWindow(width, height, "Project Odyssey"))
            {
                window.Run();
            }
        }
    }
}