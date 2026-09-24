using ProjectOdyssey.Engine;

namespace ProjectOdyssey.IO
{
    public static class ChartBinaryReader
    {
        public static ChartData ReadChartBinary(string path)
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);

            Byte version = reader.ReadByte();
            string title = reader.ReadString();
            string artist = reader.ReadString();
            string noter = reader.ReadString();
            string diffName = reader.ReadString();
            Byte keyCount = reader.ReadByte();

            var notesByColumn = new Note[keyCount][];

            for (int col = 0; col < keyCount; col++)
            {
                int noteCount = reader.ReadInt32();
                notesByColumn[col] = new Note[noteCount];

                for (int i = 0; i < noteCount; i++)
                {
                    notesByColumn[col][i] = ReadNote(reader);
                }
            }

            return new ChartData(title, artist, noter, diffName, keyCount, notesByColumn);
        }

        private static Note ReadNote(BinaryReader reader)
        {
            return new Note
            {
                NoteType = (NoteType)reader.ReadByte(),
                Column = reader.ReadByte(),
                StartTime = reader.ReadSingle(),
                EndTime = reader.ReadSingle(),
                NoteState = NoteState.Waiting, // not stored — always starts here
                HeadPosY = 0f,                 // not stored — computed live at runtime
                TailPosY = 0f                  // not stored — computed live at runtime
            };
        }
    }
}
