using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Diagnostic;
using ProjectOdyssey.Engine;
using ProjectOdyssey.Engine.Scoring;
using ProjectOdyssey.Input;
using ProjectOdyssey.IO;
using ProjectOdyssey.Render;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey.Screens
{
    public class ReplayScreen : IGameScreen
    {
        private readonly ChartData chartData;
        private readonly ReplayData replayData;
        private string songPath;
        private bool audioStarted = false;
        private ReplaySession session = null!;
        private GameplayView gameplayRenderer = null!;
        private HudView hudRenderer = null!;

        public Renderer Renderer { private get; set; }
        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }
        private readonly SyncProbe syncProbe = new(isEnabled: false);

        public ReplayScreen(ChartData chartData, ReplayData replayData, string songPath, Renderer renderer, ScreenManager screenManager, AudioManager audioManager, SkinManager skinManager)
        {
            this.chartData = chartData;
            this.replayData = replayData;
            this.songPath = songPath;
            Renderer = renderer;
            ScreenManager = screenManager;
            AudioManager = audioManager;
            SkinManager = skinManager;
        }

        public void Load()
        {
            session = new ReplaySession(replayData, chartData);

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
                AudioManager.StopAudio();
                ScreenManager.Replace(new ResultsScreen(replayData, chartData, Renderer, ScreenManager, AudioManager, SkinManager));
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
