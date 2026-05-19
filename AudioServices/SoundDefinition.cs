using DBF.Helpers;

using System.Collections;
using System.IO;
using System.Text.Json.Serialization;

namespace DBF.AudioServices
{
    /// <summary>
    /// This reads sound definitions from the embedded resources and provides properties 
    /// for accessing the sound data and metadata.
    /// </summary>
    public class SoundDefinition
    {
        #region Private Fields & Properties
        private static List<SoundDefinition> _entries = new();
        private        DictionaryEntry       _entry;
        #endregion

        #region Constructors
        static SoundDefinition()
        {
            var resourceManager = Properties.Resources.ResourceManager;
            var resourceSet     = resourceManager.GetResourceSet(System.Globalization.CultureInfo.CurrentUICulture, true, true);

            // Cache entries for later use
            foreach (DictionaryEntry entry in resourceSet)
                if (entry.Value is UnmanagedMemoryStream)
                    _entries.Add(new SoundDefinition(entry));
        }

        // Public constructor for User Instantiation  based on displayName
        public SoundDefinition(string displayName)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                var key   = displayName.GetKeyForTranslation() ?? displayName;
                var found = _entries.FirstOrDefault(e => e.Key == key);

                if (found != null)
                {
                    _entry = found._entry;
                    Key = found.Key;
                    Format = found.Format;
                    DisplayName = found.DisplayName;
                    ResourceName = found.ResourceName;
                }
            }
        }

        #region Private Constructor
        // Private constructor for intern Instantiation from the static constructor
        private SoundDefinition(DictionaryEntry entry)
        {
            _entry = entry;
            Key = entry.Key.ToString();
            Format = Stream.GetAudioFormat().ToLowerInvariant();
            DisplayName = Key.GetTranslation();
            ResourceName = $"{nameof(Properties.Resources)}.{Key}";
        }
        #endregion
        #endregion

        #region Public Properties
        public string Key { get; private set; }

        [JsonIgnore] public string DisplayName { get; private set; }
        [JsonIgnore] public string Format { get; private set; }
        [JsonIgnore] public string TempFilePath { get; set; }
        [JsonIgnore] public string ResourceName { get; private set; }

        [JsonIgnore] public UnmanagedMemoryStream Stream => (UnmanagedMemoryStream)_entry.Value;
        #endregion

        public override string ToString()
        {
            return $"{DisplayName} - {Format}";
        }
    }
}
