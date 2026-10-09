using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Engine;
using ProjectOdyssey.Input;
using ProjectOdyssey.IO;
using ProjectOdyssey.Render;
using ProjectOdyssey.Skinning;
using System.Runtime.CompilerServices;

namespace ProjectOdyssey.Screens
{
    public class GameplayScreen : IGameScreen
    {
        private readonly ChartData chartData;
        private readonly InputHistory inputHistory;
        private GameSession session = null!;
        private GameplayRenderer gameplayRenderer = null!;
        private HudRenderer hudRenderer = null!;
        private string songPath;
        private bool audioStarted = false;  

        public ScreenManager ScreenManager { private get; set; }
        public AudioManager AudioManager { private get; set; }
        public SkinManager SkinManager { private get; set; }

        public GameplayScreen(ChartData chartData, string songPath, InputHistory inputHistory)
        {
            this.chartData = chartData;
            this.songPath = songPath;
            this.inputHistory = inputHistory;
        }

        public void Load()
        {
            session = new GameSession(inputHistory, chartData);
            gameplayRenderer = new GameplayRenderer(SkinManager.Gameplay);
            gameplayRenderer.Intitialise();
            hudRenderer = new HudRenderer(SkinManager.Hud);
            hudRenderer.Initialise();
            AudioManager.ReadAudioFile(songPath);
            session.Start(chartData);
        }

        public void Update(float deltaMs)
        {
            if (!audioStarted && session.AudioReadyToStart)
            {
                AudioManager.PlayAudio(songPath);
                audioStarted = true;
            }
        }

        public void Render()
        {
            gameplayRenderer.DrawGameplay(session.NotesByColumn, session.ColumnCursors);
            hudRenderer.DrawHud(session.Combo, session.CurrentJudgementResult, session.GetRecentJudgementResults(), session.CurrentSongTimeMs, session.Accuracy);
        }

        public void Resize(int width, int height)
        {
            gameplayRenderer.UpdateViewportSize(width, height);
            hudRenderer?.Resize(width, height);
        }

        public void Unload()
        {
            session.Stop();
            gameplayRenderer.Dispose();
            hudRenderer.Dispose();
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
    }
}
