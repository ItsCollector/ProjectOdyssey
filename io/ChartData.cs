using System.Runtime.CompilerServices;

namespace ProjectOdyssey
{
    public class ChartData
    {
        public int chartId { get; set; } 
        public string? audioFilePath { get; set; } 
        public string? audioFileName { get; set; }
        public string? title { get; set; }
        public string? artist { get; set; }
        public string? noter { get; set; }
        public string? diffName { get; set; }
        public byte keyCount { get; set; } 
        public Note[][]? notesByColumn { get; set; }

        public void DisplayInfo()
        {
            Console.WriteLine($"Chart ID: {chartId}");
            Console.WriteLine($"Audio File Path: {audioFilePath}");
            Console.WriteLine($"Audio File Name: {audioFileName}");
            Console.WriteLine($"Title: {title}");
            Console.WriteLine($"Artist: {artist}");
            Console.WriteLine($"Noter: {noter}");
            Console.WriteLine($"Difficulty Name: {diffName}");
            Console.WriteLine($"Key Count: {keyCount}");
        }
    }
}
