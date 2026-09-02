using Microsoft.Data.Sqlite;

namespace ProjectOdyssey
{
    public class ChartDatabase
    {
        private readonly string connectionString;

        public ChartDatabase()
        {
            string dbPath = Path.Combine(AppContext.BaseDirectory, "chart_library.db");
            connectionString = $"Data Source={dbPath}";

            using var connection = OpenConnection();

            using var command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS ChartSets (
                    SetId INTEGER PRIMARY KEY AUTOINCREMENT,
                    FolderPath TEXT NOT NULL UNIQUE,   -- the osu set folder, or your own native import folder
                    Source TEXT NOT NULL               -- 'Native' or 'OsuLink'
                );

                CREATE TABLE IF NOT EXISTS Songs (
                    SongId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SetId INTEGER NOT NULL REFERENCES ChartSets(SetId) ON DELETE CASCADE,
                    AudioPath TEXT NOT NULL,           -- absolute path, unique WITHIN a set, not globally
                    Title TEXT NOT NULL,
                    Artist TEXT NOT NULL,
                    UNIQUE(SetId, AudioPath)
                );

                CREATE TABLE IF NOT EXISTS Charts (
                    ChartId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SongId INTEGER NOT NULL REFERENCES Songs(SongId) ON DELETE CASCADE,
                    FilePath TEXT NOT NULL UNIQUE,      -- the .json chart file
                    DiffName TEXT NOT NULL,
                    Noter TEXT NOT NULL,
                    KeyCount INTEGER NOT NULL,
                    FileLastWriteUtc INTEGER NOT NULL
                );
            ";

            command.ExecuteNonQuery();
        }

        private SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var pragmaCommand = connection.CreateCommand();
            pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
            pragmaCommand.ExecuteNonQuery();

            return connection;
        }

        

        public List<ChartRecord> GetAllCharts()
        {
            var charts = new List<ChartRecord>();

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
            @"
                SELECT ChartId, SongId, FilePath, DiffName, Noter, KeyCount, FileLastWriteUtc
                FROM Charts;
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                charts.Add(new ChartRecord
                {
                    ChartId = reader.GetInt32(0),
                    SongId = reader.GetInt32(1),
                    FilePath = reader.GetString(2),
                    DiffName = reader.GetString(4),
                    Noter = reader.GetString(5),
                    KeyCount = reader.GetInt32(6),
                    FileLastWriteUtc = reader.GetInt64(7)
                });
            }

            return charts;
        }
    }

    public class ChartRecord
    {
        public int ChartId { get; set; }
        public int SongId { get; set; }
        public string FilePath { get; set; } = "";
        public string DiffName { get; set; } = "";
        public string Noter { get; set; } = "";
        public int KeyCount { get; set; }
        public long FileLastWriteUtc { get; set; }
    }
}