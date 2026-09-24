using ProjectOdyssey.Engine;

namespace ProjectOdyssey.IO
{
    public class ChartData
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Noter { get; set; }
        public string DiffName { get; set; }
        public byte KeyCount { get; set; }
        public Note[][] NotesByColumn { get; set; }

        public ChartData(string title, string artist, string noter, string diffName, byte keyCount, Note[][] notesByColumn)
        {
            this.Title = title;
            this.Artist = artist;
            this.Noter = noter;
            this.DiffName = diffName;
            this.KeyCount = keyCount;
            this.NotesByColumn = notesByColumn;
        }

        public void DisplayInfo()
        {
            Console.WriteLine($"Title: {Title}");
            Console.WriteLine($"Artist: {Artist}");
            Console.WriteLine($"Noter: {Noter}");
            Console.WriteLine($"Difficulty Name: {DiffName}");
            Console.WriteLine($"Key Count: {KeyCount}");
        }
    }
}