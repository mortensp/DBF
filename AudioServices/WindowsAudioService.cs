//using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using Caliburn.Micro;
using DBF.Helpers;

namespace DBF.AudioServices
{
    public class WindowsAudioService : IAudioService, IDisposable
    {
        private static   NetCoreAudio.Player _player        = new();
        private static   List <string>       _tempFilePaths = new();
        private          int                 _volume;
        private readonly object              _sync          = new object();

        public bool IsPlaying => _player.Playing;

        static WindowsAudioService()
        {
        }

        public void Dispose()
        {
            _player.Stop();
            _player = null;
        }

        public int Volume
        {
            get => _volume;
            set
            {
                _volume = (int)Math.Clamp(value, 0, 100);
                _player.SetVolume((byte)_volume);
            }
        }

        public void Play(string sound) => Play(sound, 50);

        public void Play(SoundDefinition sound) => Play(sound.Key, 50);

        public void Play(SoundDefinition sound, int volume) => Play(sound.Key, volume);

        public void Play(string soundName, int volume)
        {
            try
            {
                string filePath = "";

                if (IsValidFilePath(soundName))
                    if (File.Exists(soundName))
                        filePath = soundName;
                    else
                    {
                        Logger.Error($"{Lex.MissingSoundFile}: '{soundName}'");
                        return;
                    }
                else
                    lock (_sync)
                    {
                        var resourcesObject = Properties.Resources.ResourceManager.GetObject(soundName);

                        if (resourcesObject is byte[] filebytes)
                        {
                            filePath = Path.Combine(Path.GetTempPath(), $"{soundName}.mp3");

                            if (!_tempFilePaths.Contains(filePath))
                            {
                                File.WriteAllBytes(filePath, filebytes);
                                _tempFilePaths.Add(filePath);
                            }
                        }
                        else
                            if (resourcesObject is Stream)
                                using (System.IO.Stream stream = (System.IO.Stream)resourcesObject)
                                {
                                    filePath = Path.Combine(Path.GetTempPath(), $"{soundName}.{GetAudioFormat(stream).ToLowerInvariant()}");

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

                _player.Stop();
                Volume = volume;
                _player.Play(filePath);
            }

            catch (Exception ex)
            {
                Logger.Exception(ex, $"{Lex.ErrorPlayingSound}:'{soundName}'");
                return;
            }
        }

        public void Pause()
        {
            _player.Pause();
        }

        public void Stop()
        {
            _player.Stop();
        }

        public void Resume()
        {
            _player.Resume();
        }

        #region Private Methods
            private string GetAudioFormat(Stream stream)
            {
                byte[] buffer = new byte[12]; // Læser de første bytes
                stream.Read(buffer, 0, buffer.Length);

                string header = BitConverter.ToString(buffer);

                stream.Position = 0;

                if (header.StartsWith("49-44-33")
                ||  header.StartsWith("FF")) // ID3-tag for MP3
                    return "MP3";
                else
                    if (header.StartsWith("52-49-46-46")) // RIFF-header for WAV
                        return "WAV";

                return Lex.UnknownFormat;
            }

            private bool IsValidFilePath(string path)
            {
                return  !string.IsNullOrWhiteSpace(path) && 
                       path.IndexOfAny(Path.GetInvalidPathChars()) == -1 && 
                       File.Exists(path);
            }
        #endregion
    }
}
