//using NAudio.Wave;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBF.AudioServices
{
    public static class AudioResources
    {
        public static List <string> Sounds = new();

        static AudioResources()
        {
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
    }
}
