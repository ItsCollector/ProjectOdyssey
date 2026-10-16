using ProjectOdyssey.Engine;

namespace ProjectOdyssey.IO
{
    public class ChartData
    {
        public int SetId { get; set; }
        public int ChartId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Noter { get; set; }
        public string DiffName { get; set; }
        public byte KeyCount { get; set; }
        public Note[][] NotesByColumn { get; set; }

        public ChartData(int setId, int chartId, string title, string artist, string noter, string diffName, byte keyCount, Note[][] notesByColumn)
        {
            SetId = setId;
            ChartId = chartId;
            Title = title;
            Artist = artist;
            Noter = noter;
            DiffName = diffName;
            KeyCount = keyCount;
            NotesByColumn = notesByColumn;
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