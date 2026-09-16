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
                noteType = (NoteType)reader.ReadByte(),
                column = reader.ReadByte(),
                startTime = reader.ReadSingle(),
                endTime = reader.ReadSingle(),
                noteState = NoteState.Waiting, // not stored — always starts here
                headPosY = 0f,                 // not stored — computed live at runtime
                tailPosY = 0f                  // not stored — computed live at runtime
            };
        }
    }
}
