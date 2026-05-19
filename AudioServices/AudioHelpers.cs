using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBF.AudioServices
{
    public static class AudioHelpers
    {
        public static string GetAudioFormat(this Stream stream)
        {
            byte[] buffer = new byte[12]; // read the first 12 bytes
            stream.Read(buffer, 0, buffer.Length);

            string header = BitConverter.ToString(buffer);

            stream.Position = 0;

            if (header.StartsWith("49-44-33")
            ||  header.StartsWith("FF")) // ID3-tag for MP3
                return "mp3";
            else
                if (header.StartsWith("52-49-46-46")) // RIFF-header for WAV
                    return "wav";

            return Lex.UnknownFormat.ToLowerInvariant();
        }
    }
}
