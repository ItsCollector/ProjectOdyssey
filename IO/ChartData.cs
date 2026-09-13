using ProjectOdyssey.Engine;

namespace ProjectOdyssey.IO
{
    public class ChartData
    {
        public string title { get; set; }
        public string artist { get; set; }
        public string noter { get; set; }
        public string diffName { get; set; }
        public byte keyCount { get; set; }
        public Note[][] notesByColumn { get; set; }

        public ChartData(string title, string artist, string noter, string diffName, byte keyCount, Note[][] notesByColumn)
        {
            this.title = title;
            this.artist = artist;
            this.noter = noter;
            this.diffName = diffName;
            this.keyCount = keyCount;
            this.notesByColumn = notesByColumn;
        }

        public void DisplayInfo()
        {
            Console.WriteLine($"Title: {title}");
            Console.WriteLine($"Artist: {artist}");
            Console.WriteLine($"Noter: {noter}");
            Console.WriteLine($"Difficulty Name: {diffName}");
            Console.WriteLine($"Key Count: {keyCount}");
        }
    }
}