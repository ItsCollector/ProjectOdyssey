using Microsoft.Data.Sqlite;
using ProjectOdyssey.Engine.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectOdyssey.Engine;
using ProjectOdyssey.Common;

namespace ProjectOdyssey.IO
{
    class ScoreDatabase
    {
        private static readonly string connectionString;

        static ScoreDatabase()
        {
            string dbPath = Path.Combine(AppContext.BaseDirectory, "scores.db");
            connectionString = $"Data Source={dbPath}";

            using var connection = OpenConnection();

            using (var walCommand = connection.CreateCommand())
            {
                walCommand.CommandText = "PRAGMA journal_mode=WAL;";
                walCommand.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS Scores (
                        ScoreId INTEGER PRIMARY KEY AUTOINCREMENT,
                        SetId INTEGER NOT NULL,
                        ChartId INTEGER NOT NULL,
                        Accuracy REAL NOT NULL,
                        MarvCount INTEGER NOT NULL,
                        PerfCount INTEGER NOT NULL,
                        GreatCount INTEGER NOT NULL,
                        GoodCount INTEGER NOT NULL,
                        BadCount INTEGER NOT NULL,
                        MissCount INTEGER NOT NULL,
                        HighestCombo INTEGER NOT NULL,
                        PauseCount INTEGER NOT NULL,
                        ReplayPath TEXT NOT NULL,
                        PlayedUtc INTEGER NOT NULL
                    );
                ";
                command.ExecuteNonQuery();
            }
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

        public static void SaveScore(ScoreData scoreData, int setId, int chartId, string replayPath)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            Console.WriteLine($"[INFO] Details: setID: {setId}, chartId: {chartId}, replayPath: {replayPath}");

            try
            {
                using (var insertScoreCommand = connection.CreateCommand())
                {
                    insertScoreCommand.Transaction = transaction;
                    insertScoreCommand.CommandText =
                    @"
                        INSERT INTO Scores(SetId, ChartId, Accuracy, MarvCount, PerfCount, GreatCount, GoodCount, BadCount, MissCount, HighestCombo, PauseCount, ReplayPath, PlayedUtc)
                        VALUES ($setId, $chartId, $accuracy, $marvCount, $perfCount, $greatCount, $goodCount, $badCount, $missCount, $highestCombo, $pauseCount, $replayPath, $playedUtc);
                    ";
                    insertScoreCommand.Parameters.AddWithValue("$setId", setId);
                    insertScoreCommand.Parameters.AddWithValue("$chartId", chartId);
                    insertScoreCommand.Parameters.AddWithValue("$accuracy", scoreData.CurrentContinuousAccuracy);
                    insertScoreCommand.Parameters.AddWithValue("$marvCount", scoreData.JudgementCounts[JudgementType.Marvellous]);
                    insertScoreCommand.Parameters.AddWithValue("$perfCount", scoreData.JudgementCounts[JudgementType.Perfect]);
                    insertScoreCommand.Parameters.AddWithValue("$greatCount", scoreData.JudgementCounts[JudgementType.Great]);
                    insertScoreCommand.Parameters.AddWithValue("$goodCount", scoreData.JudgementCounts[JudgementType.Good]);
                    insertScoreCommand.Parameters.AddWithValue("$badCount", scoreData.JudgementCounts[JudgementType.Bad]);
                    insertScoreCommand.Parameters.AddWithValue("$missCount", scoreData.JudgementCounts[JudgementType.Miss]);
                    insertScoreCommand.Parameters.AddWithValue("$highestCombo", scoreData.HighestCombo);
                    insertScoreCommand.Parameters.AddWithValue("$pauseCount", scoreData.PauseCount);
                    insertScoreCommand.Parameters.AddWithValue("$replayPath", replayPath);
                    insertScoreCommand.Parameters.AddWithValue("$playedUtc", scoreData.SetTimeUtc);
                    insertScoreCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw; // caller decides how to log/report a failed set
            }
        }

        public static Result<string> GetBestReplayByChartId(int chartId)
        {
            try
            {
                using var connection = OpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT ReplayPath FROM Scores
                    WHERE ChartId = $chartId
                    ORDER BY Accuracy DESC
                    LIMIT 1;
                ";
                command.Parameters.AddWithValue("$chartId", chartId);

                using var reader = command.ExecuteReader();
                return reader.Read()
                    ? Result<string>.Ok(reader.GetString(0))
                    : Result<string>.Err($"No scores found for chart {chartId}.");
            }
            catch (Exception ex)
            {
                return Result<string>.Err($"Failed to retrieve score record with ChartID {chartId}: {ex}");
            }
        }
    }
}
