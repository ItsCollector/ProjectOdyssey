namespace ProjectOdyssey
{
    public static class ChartImporter
    {
        public static Result<ChartData> Import(string filePath)
        {
            ChartData chartData = new ChartData();
            IEnumerable<string> lines;
            bool inNotes = false;

            // attempt to read lines of inputted file
            try
            {
                lines = File.ReadLines(filePath);
            }
            catch (Exception ex) 
            {
                // exit on failure
                return Result<ChartData>.Err($"Error reading file: {ex.Message}");
            }

            var notes = new List<Note>();

            foreach (string line in lines)
            {
                // Metadata 
                if (line.StartsWith("AudioFilename:"))
                {
                    chartData.audioFileName = ExtractValue(line);
                }
                if (line.StartsWith("Mode:"))
                {
                    int mode = Int32.Parse(ExtractValue(line));

                    // Skip non-mania charts
                    if (mode != 3)
                    {
                        return Result<ChartData>.Err("Unsupported mode");
                    }
                }
                if (line.StartsWith("Title:"))
                {
                    chartData.title = ExtractValue(line);
                }
                if (line.StartsWith("Artist:"))
                {
                    chartData.artist = ExtractValue(line);
                }
                if (line.StartsWith("Creator:"))
                {
                    chartData.noter = ExtractValue(line);
                }
                if (line.StartsWith("Version:"))
                {
                    chartData.diffName = ExtractValue(line);
                }
                if (line.StartsWith("CircleSize:"))
                {
                    chartData.keyCount = Byte.Parse(ExtractValue(line));
                }

                if (line.StartsWith("[HitObjects]"))
                {
                    inNotes = true;
                    continue;
                }

                if (inNotes)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                    {
                        Console.WriteLine($"[Debug] Exiting HitObjects section. Triggering line: '{line}'");
                        inNotes = false;
                    }
                    else
                    {
                        notes.Add(ParseNote(line, chartData.keyCount));
                        continue;
                    }
                }
            }

            var grouped = new List<Note>[chartData.keyCount];

            // Initialise lists for each column
            for (int i = 0; i < chartData.keyCount; i++)
            {
                grouped[i] = new List<Note>();
            }

            // Distribute notes into their respective columns
            foreach (var note in notes)
            {
                grouped[note.column].Add(note);
            }

            // Sort notes in each column by start time and convert to array
            chartData.notesByColumn = new Note[chartData.keyCount][];
            for (int i = 0; i < chartData.keyCount; i++)
            {
                grouped[i].Sort((a, b) => a.startTime.CompareTo(b.startTime)); // ensure time order per column
                chartData.notesByColumn[i] = grouped[i].ToArray();
            }

            return Result<ChartData>.Ok(chartData);
        }

        public static Note ParseNote(string line, byte keyCount)
        {
            var parts = line.Split(',');
            int x = int.Parse(parts[0]);
            int time = int.Parse(parts[2]);
            int type = int.Parse(parts[3]);
            int endTime = time;

            if (type == 128)
            {
                var lnParts = parts[5].Split(':');
                endTime = int.Parse(lnParts[0]);
            }

            return new Note
            {
                noteType = type == 128 ? NoteType.Long : NoteType.Tap,
                noteState = NoteState.Waiting,
                column = (byte)(x * keyCount / 512),
                startTime = time,
                endTime = endTime,
                headPosY = -20f,
                tailPosY = -20f
            };
        }

        public static (int tapCount, int longCount) CountNoteObjects(ChartData chartData)
        {
            int tapCount = 0;
            int longCount = 0;

            foreach (var column in chartData.notesByColumn)
            {
                if (column == null) continue;

                tapCount += column.Count(n => n.noteType == NoteType.Tap);
                longCount += column.Count(n => n.noteType == NoteType.Long);
            }

            return (tapCount, longCount);
        }

        public static string ExtractValue(string line) => line.Split(':')[1].Trim();
    }
}
