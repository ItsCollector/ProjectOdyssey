using Microsoft.Data.Sqlite;

namespace ProjectOdyssey.IO
{
    public static class ChartDatabase
    {
        private static readonly string connectionString;

        static ChartDatabase()
        {
            string dbPath = Path.Combine(AppContext.BaseDirectory, "chart_library.db");
            connectionString = $"Data Source={dbPath}";

            using var connection = OpenConnection();

            using (var walCommand = connection.CreateCommand())
            {
                walCommand.CommandText = "PRAGMA journal_mode=WAL;";
                walCommand.ExecuteNonQuery();
            }

            using var command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS ChartSets (
                    SetId INTEGER PRIMARY KEY AUTOINCREMENT,
                    FolderPath TEXT NOT NULL UNIQUE,
                    Source TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Songs (
                    SongId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SetId INTEGER NOT NULL REFERENCES ChartSets(SetId) ON DELETE CASCADE,
                    AudioPath TEXT NOT NULL,
                    UNIQUE(SetId, AudioPath)
                );

                CREATE TABLE IF NOT EXISTS Charts (
                    ChartId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SongId INTEGER NOT NULL REFERENCES Songs(SongId) ON DELETE CASCADE,
                    FilePath TEXT NOT NULL UNIQUE,
                    Title TEXT NOT NULL,
                    Artist TEXT NOT NULL,
                    DiffName TEXT NOT NULL,
                    Noter TEXT NOT NULL,
                    KeyCount INTEGER NOT NULL,
                    FileLastWriteUtc INTEGER NOT NULL
                );
            ";

            command.ExecuteNonQuery();
        }

        private static SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var pragmaCommand = connection.CreateCommand();
            pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
            pragmaCommand.ExecuteNonQuery();

            return connection;
        }

        // Song select: load all charts, joined with SetId for right-click action
        public static List<ChartBrowserRow> GetChartsForBrowsing()
        {
            var rows = new List<ChartBrowserRow>();

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
            @"
                SELECT c.ChartId, c.SongId, s.SetId, c.FilePath, c.Title, c.Artist, c.DiffName, c.Noter, c.KeyCount
                FROM Charts c
                JOIN Songs s ON c.SongId = s.SongId;
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new ChartBrowserRow
                {
                    ChartId = reader.GetInt32(0),
                    SongId = reader.GetInt32(1),
                    SetId = reader.GetInt32(2),
                    FilePath = reader.GetString(3),
                    Title = reader.GetString(4),
                    Artist = reader.GetString(5),
                    DiffName = reader.GetString(6),
                    Noter = reader.GetString(7),
                    KeyCount = reader.GetInt32(8)
                });
            }

            return rows;
        }

        // Audio loading via SongId
        public static SongRecord? GetSongRecordById(int songId)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
            @"
                SELECT SongId, SetId, AudioPath
                FROM Songs
                WHERE SongId = $songId;
            ";
            command.Parameters.AddWithValue("$songId", songId);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new SongRecord
                {
                    SongId = reader.GetInt32(0),
                    SetId = reader.GetInt32(1),
                    AudioPath = reader.GetString(2)
                };
            }

            return null;
        }

        // Generic chart list
        public static List<ChartRecord> GetAllCharts()
        {
            var charts = new List<ChartRecord>();

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
            @"
                SELECT ChartId, SongId, FilePath, Title, Artist, DiffName, Noter, KeyCount, FileLastWriteUtc
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
                    Title = reader.GetString(3),
                    Artist = reader.GetString(4),
                    DiffName = reader.GetString(5),
                    Noter = reader.GetString(6),
                    KeyCount = reader.GetInt32(7),
                    FileLastWriteUtc = reader.GetInt64(8)
                });
            }

            return charts;
        }

        // Imports an entire chart set (ChartSet + Songs + Charts) atomically in one transaction
        public static void ImportChartSet(ChartSet set, List<(ChartData chartData, string resolvedAudioPath, string jsonFilePath)> parsedChartsInSet)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                int setId;
                using (var insertSetCommand = connection.CreateCommand())
                {
                    insertSetCommand.Transaction = transaction;
                    insertSetCommand.CommandText =
                    @"
                        INSERT INTO ChartSets (FolderPath, Source)
                        VALUES ($folderPath, $source);
                        SELECT last_insert_rowid();
                    ";
                    insertSetCommand.Parameters.AddWithValue("$folderPath", set.FolderPath);
                    insertSetCommand.Parameters.AddWithValue("$source", set.Source);
                    setId = Convert.ToInt32(insertSetCommand.ExecuteScalar());
                }

                // Cache SongId lookups within this set so identical audio across
                // several charts doesn't re-run the same SELECT repeatedly.
                var songIdCache = new Dictionary<string, int>();

                foreach (var (chartData, resolvedAudioPath, jsonFilePath) in parsedChartsInSet)
                {
                    if (!songIdCache.TryGetValue(resolvedAudioPath, out int songId))
                    {
                        using (var selectCommand = connection.CreateCommand())
                        {
                            selectCommand.Transaction = transaction;
                            selectCommand.CommandText =
                            @"
                                SELECT SongId FROM Songs
                                WHERE SetId = $setId AND AudioPath = $audioPath;
                            ";
                            selectCommand.Parameters.AddWithValue("$setId", setId);
                            selectCommand.Parameters.AddWithValue("$audioPath", resolvedAudioPath);

                            var existing = selectCommand.ExecuteScalar();
                            if (existing != null)
                            {
                                songId = Convert.ToInt32(existing);
                            }
                            else
                            {
                                using var insertSongCommand = connection.CreateCommand();
                                insertSongCommand.Transaction = transaction;
                                insertSongCommand.CommandText =
                                @"
                                    INSERT INTO Songs (SetId, AudioPath)
                                    VALUES ($setId, $audioPath);
                                    SELECT last_insert_rowid();
                                ";
                                insertSongCommand.Parameters.AddWithValue("$setId", setId);
                                insertSongCommand.Parameters.AddWithValue("$audioPath", resolvedAudioPath);
                                songId = Convert.ToInt32(insertSongCommand.ExecuteScalar());
                            }
                        }

                        songIdCache[resolvedAudioPath] = songId;
                    }

                    using var insertChartCommand = connection.CreateCommand();
                    insertChartCommand.Transaction = transaction;
                    insertChartCommand.CommandText =
                    @"
                        INSERT INTO Charts (SongId, FilePath, Title, Artist, DiffName, Noter, KeyCount, FileLastWriteUtc)
                        VALUES ($songId, $filePath, $title, $artist, $diffName, $noter, $keyCount, $fileLastWriteUtc);
                    ";
                    insertChartCommand.Parameters.AddWithValue("$songId", songId);
                    insertChartCommand.Parameters.AddWithValue("$filePath", jsonFilePath);
                    insertChartCommand.Parameters.AddWithValue("$title", chartData.title);
                    insertChartCommand.Parameters.AddWithValue("$artist", chartData.artist);
                    insertChartCommand.Parameters.AddWithValue("$diffName", chartData.diffName);
                    insertChartCommand.Parameters.AddWithValue("$noter", chartData.noter);
                    insertChartCommand.Parameters.AddWithValue("$keyCount", chartData.keyCount);
                    insertChartCommand.Parameters.AddWithValue("$fileLastWriteUtc", DateTime.UtcNow.Ticks);
                    insertChartCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw; // caller decides how to log/report a failed set
            }
        }

        // Check if a ChartSet exists by folder path
        public static bool ChartSetExists(string folderPath)
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

        // Delete a single chart; drop its Song too if no other chart references it
        public static void DeleteChart(int chartId)
        {
            using var connection = OpenConnection();

            int songId;
            using (var getSongIdCommand = connection.CreateCommand())
            {
                getSongIdCommand.CommandText = "SELECT SongId FROM Charts WHERE ChartId = $chartId;";
                getSongIdCommand.Parameters.AddWithValue("$chartId", chartId);
                var result = getSongIdCommand.ExecuteScalar();
                if (result == null) return; // chart doesn't exist
                songId = Convert.ToInt32(result);
            }

            using (var deleteChartCommand = connection.CreateCommand())
            {
                deleteChartCommand.CommandText = "DELETE FROM Charts WHERE ChartId = $chartId;";
                deleteChartCommand.Parameters.AddWithValue("$chartId", chartId);
                deleteChartCommand.ExecuteNonQuery();
            }

            using (var countCommand = connection.CreateCommand())
            {
                countCommand.CommandText = "SELECT COUNT(*) FROM Charts WHERE SongId = $songId;";
                countCommand.Parameters.AddWithValue("$songId", songId);
                long remaining = (long)countCommand.ExecuteScalar()!;

                if (remaining == 0)
                {
                    using var deleteSongCommand = connection.CreateCommand();
                    deleteSongCommand.CommandText = "DELETE FROM Songs WHERE SongId = $songId;";
                    deleteSongCommand.Parameters.AddWithValue("$songId", songId);
                    deleteSongCommand.ExecuteNonQuery();
                }
            }
        }

        // Delete an entire chart set; FK cascade handles Songs + Charts underneath it
        public static void DeleteChartSet(int setId)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ChartSets WHERE SetId = $setId;";
            command.Parameters.AddWithValue("$setId", setId);
            command.ExecuteNonQuery();
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
    }

    public class ChartRecord
    {
        public int ChartId { get; set; }
        public int SongId { get; set; }
        public string FilePath { get; set; } = "";
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string DiffName { get; set; } = "";
        public string Noter { get; set; } = "";
        public int KeyCount { get; set; }
        public long FileLastWriteUtc { get; set; }
    }

    // Similar to the ChartRecord but contains SetId for right-click actions in the chart browser
    public class ChartBrowserRow
    {
        public int ChartId { get; set; }
        public int SongId { get; set; }
        public int SetId { get; set; }
        public string FilePath { get; set; } = "";
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string DiffName { get; set; } = "";
        public string Noter { get; set; } = "";
        public int KeyCount { get; set; }
    }
}