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

        // Assigned in Load(), which the ScreenManager guarantees runs before
        // Update()/Render()/Resize()/Unload() are ever called on this screen.
        private GameSession session = null!;
        private GameplayRenderer gameplayRenderer = null!;
        private GameplaySkinConfig skinConfig = null!;
        private SkinAssets skinAssets = null!;
        private AudioManager audioManager;
        private string songPath;
        private bool audioStarted = false;
        private bool isPaused = false;

        public GameplayScreen(ChartData chartData, string songPath, InputHistory inputHistory, AudioManager audioManager)
        {
            this.chartData = chartData;
            this.songPath = songPath;
            this.inputHistory = inputHistory;
            this.audioManager = audioManager;
        }

        public void Load()
        {
            (skinConfig, skinAssets) = GameplaySkinParser.LoadSkin().value;
            session = new GameSession(inputHistory, chartData);
            gameplayRenderer = new GameplayRenderer(skinConfig, skinAssets);
            gameplayRenderer.Intitialise();
            audioManager.ReadAudioFile(songPath);
            session.Start(chartData);
        }

        public void Update(float deltaMs)
        {
            if (!audioStarted && session.AudioReadyToStart)
            {
                audioManager.PlayAudio();
                audioStarted = true;
            }
        }

        public void Render()
        {
            gameplayRenderer.DrawGameplay(session.notesByColumn, session.columnCursors);
        }

        public void Resize(int width, int height)
        {
            gameplayRenderer.UpdateViewportSize(width, height);
        }

        public void Unload()
        {
            session.Stop();
            gameplayRenderer.Dispose();
        }

        public void OnKeyDown(Keys key)
        {
            if (key == Keys.Escape)
            {
                PauseGame();
            }
        }

        public void PauseGame()
        {
            if (isPaused)
            {
                session.Resume();
                audioManager.ResumeAudio();
            }
            else
            {
                session.Pause();
                audioManager.PauseAudio();
            }
            isPaused = !isPaused;
        }
    }
}
