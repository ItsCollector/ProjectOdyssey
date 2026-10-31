using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Input;
using ProjectOdyssey.Skinning;
using ProjectOdyssey.Diagnostic;
using ProjectOdyssey.Rendering;
using ProjectOdyssey.App;
using ProjectOdyssey.Data.Replays;
using ProjectOdyssey.Engine.Chart;
using ProjectOdyssey.Engine.Sessions;
using ProjectOdyssey.Data.Scores;
using ProjectOdyssey.Screens.Results;
using ProjectOdyssey.Screens.Pause;

namespace ProjectOdyssey.Screens.Gameplay
{
    public class GameplayScreen : IGameScreen
    {
        private readonly ChartData chartData;
        private readonly InputHistory inputHistory;
        private GameSession session = null!;
        private GameplayView gameplayRenderer = null!;
        private HudView hudRenderer = null!;
        private string songPath;
        private bool audioStarted = false;

        private const float resultsDelayMs = 2000f;
        private bool finished = false;
        private float msSinceFinished;
        private ReplayData? finishedReplayData;

        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }
        private readonly SyncProbe syncProbe = new(isEnabled: false);

        public GameplayScreen(ChartData chartData, string songPath, InputHistory inputHistory, Renderer renderer, ScreenManager screenManager, AudioManager audioManager, SkinManager skinManager)
        {
            this.chartData = chartData;
            this.songPath = songPath;
            this.inputHistory = inputHistory;
            Renderer = renderer;
            ScreenManager = screenManager;
            AudioManager = audioManager;
            SkinManager = skinManager;
        }

        public void Load()
        {
            session = new GameSession(
                inputHistory,
                chartData);

            gameplayRenderer = new GameplayView(
                SkinManager.GameplaySkins[chartData.KeyCount],
                Renderer,
                session.ApproachTime,
                session.SpawnPositionY);

            hudRenderer = new HudView(
                SkinManager.HudSkins[chartData.KeyCount],
                Renderer);

            AudioManager.ReadAudioFile(songPath);
            session.Start();
        }

        public void Update(float deltaMs)
        {
            if (!audioStarted && session.AudioReadyToStart)
            {
                AudioManager.PlayAudio(songPath);
                audioStarted = true;
            }

            if (audioStarted)
            {
                syncProbe.Sample(session.CurrentSongTimeMs, AudioManager.GetPlaybackPositionMs());
            }

            if (finished)
            {
                msSinceFinished += deltaMs;

                if (msSinceFinished >= resultsDelayMs && finishedReplayData != null)
                {
                    AudioManager.StopAudio();
                    ScreenManager.Replace(new ResultsScreen(finishedReplayData, chartData, inputHistory, Renderer, ScreenManager, AudioManager, SkinManager));
                }

                return;
            }

            if (session.ResolvedNotes >= session.TotalNotes)
            {
                var scoreData = session.Score.CompileScoreData();

                Console.WriteLine($"[INFO] Number of inputs read this play: {session.recordedInputs.Count}");
                ReplayData newReplayData = new ReplayData
                {
                    SetId = chartData.SetId,
                    ChartId = chartData.ChartId,
                    ScoreData = scoreData,
                    ReplayEvents = session.recordedInputs.ToArray(),
                    ColumnByVKey = session.ColumnByVKey
                };

                // Add score to db
                try
                {
                    string newReplayDataPath = ReplayService.WriteReplayBinary(newReplayData);
                    ScoreDatabase.SaveScore(scoreData, chartData.SetId, chartData.ChartId, newReplayDataPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] Failed to save score: {ex}");
                }

                finishedReplayData = newReplayData;
                finished = true;
                msSinceFinished = 0f;
            }
        }

        public void Render()
        {
            float now = session.CurrentSongTimeMs;
            gameplayRenderer.DrawGameplay(session.NotesByColumn, session.ColumnCursors, now);
            hudRenderer.DrawHud(session.Combo, session.CurrentJudgementResult, session.GetRecentJudgementResults(), now, session.Accuracy);
        }

        public void Unload()
        {
            syncProbe.WriteReport(Path.Combine(AppContext.BaseDirectory, "Profiling"));
            session.Stop();
            AudioManager.StopAudio();
        }

        public void OnKeyDown(Keys key)
        {
            if (key == Keys.Escape && !finished)
            {
                PauseGame();
            }
        }

        private void PauseGame()
        {
            session.Pause();
            AudioManager.PauseAudio();
            ScreenManager.Push(new PauseScreen(Renderer, ScreenManager, AudioManager, SkinManager));
        }

        public void OnResume()
        {
            session.Resume();
            AudioManager.ResumeAudio();
        }
            
        public void OnRestart()
        {

            AudioManager.StopAudio();
            session.Restart();
            audioStarted = false;
        }
    }
}
