//#define NAUDIO
using System.IO;
using DBF.Helpers;

namespace DBF.AudioServices
{
    public class WindowsAudioService : IAudioService, IDisposable
    {
#if NAUDIO
        private static NAudioPlayer _player = new();
        public bool IsPlaying => _player.IsPlaying;
#else
        private static NetCoreAudio.Player _player = new();
        public bool IsPlaying => _player.Playing;
#endif
        private static   List <string> _tempFilePaths = new();
        private          int           _volume;
        private readonly object        _sync          = new object();
        private          string        filePath ;

        static WindowsAudioService()
        {
        }

        public void Dispose()
        {
            _       = _player.Stop();
            _player = null;
        }

        public int Volume
        {
            get => _volume;
            set
            {
                _volume = (int)Math.Clamp(value, 0, 100);

#if NAUDIO
                _player.Volume = _volume / 100f;
#else
                _ = _player.SetVolume((byte)_volume);
#endif
            }
        }

        public void Play(SoundDefinition sound) => Play(sound, 60);

        public void Play(SoundDefinition sound, int volume)
        {
            try
            {
                _ = _player.Stop();

                lock (_sync)
                {
                    using (System.IO.Stream stream = (System.IO.Stream)sound.Stream)
                    {
                        filePath = Path.Combine(Path.GetTempPath(), $"{sound.Key}.{sound.Format}");

                        if (!_tempFilePaths.Contains(filePath))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                stream.CopyTo(memoryStream);
                                File.WriteAllBytes(filePath, memoryStream.ToArray());
                            }

                            _tempFilePaths.Add(filePath);
                        }
                    }
                }

                Volume = volume;
                _      = _player.Play(filePath);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"{Lex.ErrorPlayingSound}:'{sound.Key}'");
                return;
            }
        }

        public void Pause()
        {
            _ = _player.Pause();
        }

        public void Stop()
        {
            _ = _player.Stop();
        }

        public void Resume()
        {
            _ = _player.Resume();
        }

        #region Private Methods
            private bool IsValidFilePath(string path)
            {
                return  !string.IsNullOrWhiteSpace(path) && 
                       path.IndexOfAny(Path.GetInvalidPathChars()) == -1 && 
                       File.Exists(path);
            }
        #endregion
    }
}
