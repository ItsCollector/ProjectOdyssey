namespace ProjectOdyssey
{
    public static class OsuChartParser
    {
        // Generic entry point: parse a .osu file at an already-known absolute
        // path. Used directly for test charts / non-osu-library imports, and
        // internally by ImportFromOsuLibrary once the path is resolved.
        public static Result<ChartData> OsuToChartData(string filePath)
        {
            string audioFileName = string.Empty;
            string title = string.Empty;
            string artist = string.Empty;
            string noter = string.Empty;
            string diffName = string.Empty;
            byte keyCount = 0;
            bool keyCountSet = false;

            IEnumerable<string> lines;
            bool inNotes = false;

            try
            {
                lines = File.ReadLines(filePath);
            }
            catch (Exception ex)
            {
                return Result<ChartData>.Err($"Error reading file: {ex.Message}");
            }

            var notes = new List<Note>();

            foreach (string line in lines)
            {
                if (line.StartsWith("[HitObjects]"))
                {
                    inNotes = true;
                    continue;
                }

                if (inNotes)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                    {
                        inNotes = false;
                    }
                    else
                    {
                        if (!keyCountSet)
                        {
                            return Result<ChartData>.Err("HitObjects encountered before CircleSize was set");
                        }
                        notes.Add(ParseNote(line, keyCount));
                    }
                    continue;
                }

                if (line.StartsWith("AudioFilename:"))
                {
                    audioFileName = ExtractValue(line);
                }
                else if (line.StartsWith("Mode:"))
                {
                    int mode = int.Parse(ExtractValue(line));
                    if (mode != 3)
                    {
                        return Result<ChartData>.Err("Unsupported mode");
                    }
                }
                else if (line.StartsWith("Title:"))
                {
                    title = ExtractValue(line);
                }
                else if (line.StartsWith("Artist:"))
                {
                    artist = ExtractValue(line);
                }
                else if (line.StartsWith("Creator:"))
                {
                    noter = ExtractValue(line);
                }
                else if (line.StartsWith("Version:"))
                {
                    diffName = ExtractValue(line);
                }
                else if (line.StartsWith("CircleSize:"))
                {
                    keyCount = byte.Parse(ExtractValue(line));
                    keyCountSet = true;
                }
            }

            if (!keyCountSet)
            {
                return Result<ChartData>.Err("CircleSize was never specified");
            }

            string osuFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
            string resolvedAudioPath = Path.Combine(osuFolder, audioFileName);

            if (!File.Exists(resolvedAudioPath))
            {
                return Result<ChartData>.Err($"Audio file not found: {resolvedAudioPath}");
            }

            var grouped = new List<Note>[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                grouped[i] = new List<Note>();
            }

            foreach (var note in notes)
            {
                grouped[note.column].Add(note);
            }

            var notesByColumn = new Note[keyCount][];
            for (int i = 0; i < keyCount; i++)
            {
                grouped[i].Sort((a, b) => a.startTime.CompareTo(b.startTime));
                notesByColumn[i] = grouped[i].ToArray();
            }

            var chartData = new ChartData(resolvedAudioPath, title, artist, noter, diffName, keyCount, notesByColumn);
            return Result<ChartData>.Ok(chartData);
        }

        public static Note ParseNote(string line, byte keyCount)
        {
            var parts = line.Split(',');
            int x = int.Parse(parts[0]);
            int time = int.Parse(parts[2]);
            int type = int.Parse(parts[3]);
            int endTime = time;

            bool isLongNote = (type & 128) != 0;

            if (isLongNote)
            {
                var lnParts = parts[5].Split(':');
                endTime = int.Parse(lnParts[0]);
            }

            return new Note
            {
                noteType = isLongNote ? NoteType.Long : NoteType.Tap,
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

        public static string ExtractValue(string line)
        {
            int colonIndex = line.IndexOf(':');
            return colonIndex < 0 ? string.Empty : line[(colonIndex + 1)..].Trim();
        }
    }
}