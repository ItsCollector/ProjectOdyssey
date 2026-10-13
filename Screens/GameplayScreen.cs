using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Engine;
using ProjectOdyssey.Input;
using ProjectOdyssey.IO;
using ProjectOdyssey.Render;
using ProjectOdyssey.Skinning;
using ProjectOdyssey.Diagnostic;

namespace ProjectOdyssey.Screens
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

        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }
        private readonly SyncProbe syncProbe = new(isEnabled: false);

        public GameplayScreen(ChartData chartData, string songPath, InputHistory inputHistory)
        {
            this.chartData = chartData;
            this.songPath = songPath;
            this.inputHistory = inputHistory;
        }

        public void Load()
        {
            session = new GameSession(
                inputHistory,
                chartData,
                SkinManager.GameplaySkins[chartData.KeyCount].HitPositionY);

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

            if (session.ResolvedNotes >= session.TotalNotes)
            {
                // Transition to results screen
                var scoreData = session.Score.CompileScoreData();
                AudioManager.StopAudio();
                ScreenManager.Replace(new ResultsScreen(scoreData));
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
            if (key == Keys.Escape)
            {
                PauseGame();
            }
        }

        private void PauseGame()
        {
            session.Pause();
            AudioManager.PauseAudio();
            ScreenManager.Push(new PauseScreen());
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
