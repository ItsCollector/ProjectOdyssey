namespace ProjectOdyssey
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (MainWindow window = new MainWindow(1920, 1080, "Project Odyssey"))
            {
                window.Run();
            }
        }
    }
}
