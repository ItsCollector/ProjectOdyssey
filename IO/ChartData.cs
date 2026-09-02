namespace ProjectOdyssey
{
    public class ChartData
    {
        //public int chartId { get; set; } unused until I make db
        //public string audioFilePath { get; set; } unused until I make audio player
        public string audioName { get; set; }
        public string title { get; set; }
        public string artist { get; set; }
        public string noter { get; set; }
        public string diffName { get; set; }
        public byte keyCount { get; set; } 
        public Note[][] notesByColumn { get; set; }

        public ChartData(string audioName, string title, string artist, string noter, string diffName, Byte keyCount, Note[][] notesByColumn)
        {
            this.audioName = audioName;
            this.title = title;
            this.artist = artist;
            this.noter = noter;
            this.diffName = diffName;
            this.keyCount = keyCount;
            this.notesByColumn = notesByColumn;
        }

        public void DisplayInfo()
        {
            //Console.WriteLine($"Chart ID: {chartId}");
            //Console.WriteLine($"Audio File Path: {audioFilePath}");
            Console.WriteLine($"Audio File Path: {audioName}");
            Console.WriteLine($"Title: {title}");
            Console.WriteLine($"Artist: {artist}");
            Console.WriteLine($"Noter: {noter}");
            Console.WriteLine($"Difficulty Name: {diffName}");
            Console.WriteLine($"Key Count: {keyCount}");
        }
    }
}
