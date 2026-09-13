using Microsoft.Data.Sqlite;

namespace ProjectOdyssey.IO
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
                    DiffName = reader.GetString(3),
                    Noter = reader.GetString(4),
                    KeyCount = reader.GetInt32(5),
                    FileLastWriteUtc = reader.GetInt64(6)
                });
            }

            return charts;
        }

        public int InsertChartSet(ChartSet set)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
            @"
                INSERT INTO ChartSets (FolderPath, Source)
                VALUES ($folderPath, $source);
                SELECT last_insert_rowid();
            ";
            command.Parameters.AddWithValue("$folderPath", set.FolderPath);
            command.Parameters.AddWithValue("$source", set.Source);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int InsertChart(ChartRecord chart)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
            @"
                INSERT INTO Charts (SongId, FilePath, DiffName, Noter, KeyCount, FileLastWriteUtc)
                VALUES ($songId, $filePath, $diffName, $noter, $keyCount, $fileLastWriteUtc);
                SELECT last_insert_rowid();
            ";
            command.Parameters.AddWithValue("$songId", chart.SongId);
            command.Parameters.AddWithValue("$filePath", chart.FilePath);
            command.Parameters.AddWithValue("$diffName", chart.DiffName);
            command.Parameters.AddWithValue("$noter", chart.Noter);
            command.Parameters.AddWithValue("$keyCount", chart.KeyCount);
            command.Parameters.AddWithValue("$fileLastWriteUtc", chart.FileLastWriteUtc);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int GetOrCreateSong(SongRecord song)
        {
            using var connection = OpenConnection();

            using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.CommandText =
                @"
                    SELECT SongId
                    FROM Songs
                    WHERE SetId = $setId AND AudioPath = $audioPath;
                ";
                selectCommand.Parameters.AddWithValue("$setId", song.SetId);
                selectCommand.Parameters.AddWithValue("$audioPath", song.AudioPath);

                var existing = selectCommand.ExecuteScalar();
                if (existing != null)
                {
                    return Convert.ToInt32(existing);
                }
            }

            using var insertCommand = connection.CreateCommand();
            insertCommand.CommandText =
            @"
                INSERT INTO Songs (SetId, AudioPath, Title, Artist)
                VALUES ($setId, $audioPath, $title, $artist);
                SELECT last_insert_rowid();
            ";
            insertCommand.Parameters.AddWithValue("$setId", song.SetId);
            insertCommand.Parameters.AddWithValue("$audioPath", song.AudioPath);
            insertCommand.Parameters.AddWithValue("$title", song.Title);
            insertCommand.Parameters.AddWithValue("$artist", song.Artist);

            return Convert.ToInt32(insertCommand.ExecuteScalar());
        }

        public bool ChartSetExists(string folderPath)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
            @"
                SELECT 1
                FROM ChartSets
                WHERE FolderPath = $folderPath
                LIMIT 1;
            ";
            command.Parameters.AddWithValue("$folderPath", folderPath);

            return command.ExecuteScalar() != null;
        }
    }

    public class ChartSet
    {
        public int SetId { get; set; }
        public string FolderPath { get; set; } = "";
        public string Source { get; set; } = ""; // 'Native' or 'OsuLink'
    }

    public class SongRecord
    {
        public int SongId { get; set; }
        public int SetId { get; set; }
        public string AudioPath { get; set; } = "";
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
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