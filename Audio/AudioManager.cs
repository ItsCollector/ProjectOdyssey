using NAudio.Vorbis;
using NAudio.Wave;

namespace ProjectOdyssey.Audio
{
    public class AudioManager : IDisposable
    {
        private WasapiOut outputDevice;
        private WaveStream? reader;
        public string? LoadedPath { get; private set; }   // path currently loaded into the reader
        public string? PlayingPath { get; private set; }  // path that PlayAudio last actually started

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
            if (songPath == LoadedPath) return;

            if (string.IsNullOrEmpty(songPath) || !File.Exists(songPath))
            {
                Console.WriteLine($"[Warning] Audio file not found or invalid: {songPath}");
                return;
            }

            outputDevice?.Dispose();
            reader?.Dispose();

            outputDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 30);

            try
            {
                reader = OpenAudioStream(songPath);
                outputDevice.Init(reader);
                LoadedPath = songPath;
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"[Warning] Failed to load audio '{songPath}': {ex.GetType().Name} - {ex.Message}");
                reader = null;
                LoadedPath = null;
            }
        }

        public void PlayAudio(string songPath)
        {
            if (songPath == PlayingPath) return; // already playing this track; ignore

            if (reader != null)
            {
                reader.Position = 0;
            }

            outputDevice.Play();
            PlayingPath = songPath;
        }

        public void StopAudio()
        {
            outputDevice.Stop();
            PlayingPath = null; // nothing is playing now, so a later request for this same path shouldn't be ignored
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

        // How far the output device has actually played, in ms (0 if stopped). Used for sync diagnostics.
        public double GetPlaybackPositionMs()
        {
            var format = outputDevice.OutputWaveFormat;
            if (format == null || format.AverageBytesPerSecond == 0) return 0;

            return outputDevice.GetPosition() * 1000.0 / format.AverageBytesPerSecond;
        }
    }
}
