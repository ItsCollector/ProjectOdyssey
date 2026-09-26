using NAudio.Wave;
using NAudio.Vorbis;

namespace ProjectOdyssey.Audio
{
    public class AudioManager
    {
        private WasapiOut outputDevice;
        private WaveStream? reader;
        private string? loadedPath;   // path currently loaded into the reader
        private string? playingPath;  // path that PlayAudio last actually started

        public AudioManager()
        {
            outputDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 30);
        }

        private WaveStream OpenAudioStream(string path)
        {
            return Path.GetExtension(path).Equals(".ogg", StringComparison.OrdinalIgnoreCase)
                ? new VorbisWaveReader(path)
                : new AudioFileReader(path);
        }

        public void ReadAudioFile(string songPath)
        {
            if (songPath == loadedPath) return;

            outputDevice?.Dispose();
            reader?.Dispose();

            outputDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 30);
            reader = OpenAudioStream(songPath);

            outputDevice.Init(reader);

            loadedPath = songPath;
        }

        public void PlayAudio(string songPath)
        {
            if (songPath == playingPath) return; // already playing this track; ignore

            if (reader != null)
            {
                reader.Position = 0;
            }

            outputDevice.Play();
            playingPath = songPath;
        }

        public void StopAudio()
        {
            outputDevice.Stop();
            playingPath = null; // nothing is playing now, so a later request for this same path shouldn't be ignored
        }

        public void PauseAudio()
        {
            outputDevice.Pause();
        }

        public void ResumeAudio()
        {
            outputDevice.Play();
        }

        public void Dispose()
        {
            outputDevice?.Dispose();
            reader?.Dispose();
        }
    }
}
