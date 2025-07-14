using NAudio.Wave;
using Syncfusion.Data.Extensions;
using Syncfusion.ProjIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace DBF
{
    public class AudioPlayer
    {
        private static NetCoreAudio.Player player        = new();
        private static List <string>       tempFilePaths = new();
        public static  List <string>       Sounds        = new();

        static AudioPlayer()
        {
            // Load the sounds
            var resourceManager = Properties.Resources.ResourceManager;
            var resourceSet     = resourceManager.GetResourceSet(System.Globalization.CultureInfo.CurrentUICulture, true, true);
            var sounds          = new List<string>();

            foreach (DictionaryEntry entry in resourceSet)
            {
                if (entry.Value is UnmanagedMemoryStream stream)
                    sounds.Add(entry.Key.ToString());
            }

            Sounds = sounds.OrderBy(name => name).ToList();
        }

        public static void Play(string sound, int volume = 50)
        {
            try
            {
                string tempFilePath = "";

                if (IsValidFilePath(sound))
                    if (File.Exists(sound))
                        tempFilePath = sound;
                    else
                    {
                        Console.WriteLine($"sound file doesn't exist: '{sound}'");
                        return;
                    }
                else
                {
                    var resourcesObject = Properties.Resources.ResourceManager.GetObject(sound);

                    if (resourcesObject is byte[] filebytes)
                    {
                        tempFilePath = Path.Combine(Path.GetTempPath(), $"{sound}.mp3");
                        File.WriteAllBytes(tempFilePath, filebytes);
                    }
                    else
                        if (resourcesObject is Stream)
                            using (System.IO.Stream stream = (System.IO.Stream)resourcesObject)
                            {
                                tempFilePath = Path.Combine(Path.GetTempPath(), $"{sound}.{GetAudioFormat(stream).ToLowerInvariant()}");

                                using (var memoryStream = new MemoryStream())
                                {
                                    stream.CopyTo(memoryStream);
                                    File.WriteAllBytes(tempFilePath, memoryStream.ToArray());
                                }
                            }

                    if (!tempFilePaths.Contains(tempFilePath))
                        tempFilePaths.Add(tempFilePath);
                }

                player.Stop();
                player.SetVolume((byte)int.Clamp(volume, 0, 100));
                player.Play(tempFilePath);
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error playing sound '{sound}': {ex.Message}");
                return;
            }
        }

        public void Pause()
        {
            player.Pause();
        }

        public void Stop()
        {
            player.Stop();
        }

        #region Private Methods
            private static string GetAudioFormat(Stream stream)
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

                return "Ukendt format";
            }

            private static bool IsValidFilePath(string path)
            {
                return  !string.IsNullOrWhiteSpace(path) && 
                       path.IndexOfAny(Path.GetInvalidPathChars()) == -1 && 
                       File.Exists(path);
            }
        #endregion
    }
}