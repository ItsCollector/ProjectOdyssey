using ProjectOdyssey.Common;
using ProjectOdyssey.Engine;

namespace ProjectOdyssey.IO
{
    public static class OsuChartParser
    {
        // Generic entry point: parse a .osu file at an already-known absolute
        // path. Used directly for test charts / non-osu-library imports, and
        // internally by ImportFromOsuLibrary once the path is resolved.
        public static Result<(ChartData chartData, string resolvedAudioPath, string resolvedBackgroundPath)> OsuToChartData(string filePath)
        {
            string audioFileName = string.Empty;
            string backgroundFileName = string.Empty;
            string title = string.Empty;
            string artist = string.Empty;
            string noter = string.Empty;
            string diffName = string.Empty;
            byte keyCount = 0;
            bool keyCountSet = false;

            IEnumerable<string> lines;
            bool inNotes = false;
            bool inEvents = false;

            try
            {
                lines = File.ReadLines(filePath);
            }
            catch (Exception ex)
            {
                return Result<(ChartData chartData, string resolvedAudioPath, string resolvedBackgroundPath)>.Err($"Error reading file: {ex.Message}");
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
                            return Result<(ChartData chartData, string resolvedAudioPath, string resolvedBackgroundPath)>.Err("HitObjects encountered before CircleSize was set");
                        }
                        notes.Add(ParseNote(line, keyCount));
                    }
                    continue;
                }

                if (line.StartsWith("[Events]"))
                {
                    inEvents = true;
                    continue;
                }

                if (inEvents)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                    {
                        inEvents = false;
                    }
                    else
                    {
                        if (line.StartsWith("0,0,"))
                        {
                            var parts = line.Split(',');
                            if (parts.Length >= 3)
                            {
                                backgroundFileName = parts[2].Trim('"');
                            }
                        }
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
                        return Result<(ChartData chartData, string resolvedAudioPath, string resolvedBackgroundPath)>.Err("Unsupported mode");
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
                return Result<(ChartData chartData, string resolvedAudioPath, string resolvedBackgroundPath)>.Err("CircleSize was never specified");
            }

            if (notes.Count == 0)
            {
                return Result<(ChartData chartData, string resolvedAudioPath, string resolvedBackgroundPath)>.Err("Chart has zero notes");
            }

            string osuFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
            string resolvedAudioPath = Path.Combine(osuFolder, audioFileName);
            string resolvedBackgroundPath = Path.Combine(osuFolder, backgroundFileName);

            if (!File.Exists(resolvedAudioPath))
            {
                return Result<(ChartData chartData, string resolvedAudioPath, string resolvedBackgroundPath)>.Err($"Audio file not found: {resolvedAudioPath}");
            }

            if (!File.Exists(resolvedBackgroundPath))
            {
                resolvedBackgroundPath = Path.Combine(AppContext.BaseDirectory, "Assets\\Chart Browser\\missing_background_image.png");
            }

            var grouped = new List<Note>[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                grouped[i] = new List<Note>();
            }

            foreach (var note in notes)
            {
                grouped[note.Column].Add(note);
            }

            var notesByColumn = new Note[keyCount][];
            for (int i = 0; i < keyCount; i++)
            {
                grouped[i].Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                notesByColumn[i] = grouped[i].ToArray();
            }

            var chartData = new ChartData(title, artist, noter, diffName, keyCount, notesByColumn);
            return Result<(ChartData chartData, string resolvedAudioPath, string resolvedBackgroundPath)>.Ok((chartData, resolvedAudioPath, resolvedBackgroundPath));
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
                NoteType = isLongNote ? NoteType.Long : NoteType.Tap,
                NoteState = NoteState.Waiting,
                Column = (byte)(x * keyCount / 512),
                StartTime = time,
                EndTime = endTime,
                HeadPosY = -20f,
                TailPosY = -20f
            };
        }

        public static (int tapCount, int longCount) CountNoteObjects(ChartData chartData)
        {
            int tapCount = 0;
            int longCount = 0;

            foreach (var column in chartData.NotesByColumn)
            {
                if (column == null) continue;
                tapCount += column.Count(n => n.NoteType == NoteType.Tap);
                longCount += column.Count(n => n.NoteType == NoteType.Long);
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