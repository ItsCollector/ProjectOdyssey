using NAudio.Wave;

namespace ProjectOdyssey.Audio
{
    public class AudioManager
    {
        private WasapiOut outputDevice;
        private AudioFileReader? reader;

        // Look into MixingSampleProvider for mixing sounds (UI SFX on top of music) later

        public AudioManager()
        {
            outputDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 30);
        }

        public void ReadAudioFile(string songPath)
        {
            outputDevice?.Stop();
            reader?.Dispose();
            reader = new AudioFileReader(songPath);
            outputDevice?.Init(reader);
        }

        public void PlayAudio()
        {
            if (reader != null)
            {
                // Reset playback position so the track starts from the beginning
                reader.Position = 0;
            }

            outputDevice.Play();
        }

        public void StopAudio()
        {
            outputDevice.Stop();
        }

        public void PauseAudio()
        {
            outputDevice.Pause();
        }

        public void Dispose()
        {
            if (outputDevice != null)
            {
                outputDevice.Dispose();
            }

            if (reader != null)
            {
                reader.Dispose();
            }
        }
    }
}
