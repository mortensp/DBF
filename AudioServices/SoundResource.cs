using System.Collections;

using System.IO;
using System.Text.Json.Serialization;
using DBF.Helpers;
using String.Localization;

namespace DBF.AudioServices
{
    public class SoundResource
    {
        #region Private Fields & Properties
            private DictionaryEntry _entry;
        #endregion

        #region Constructors
            public SoundResource(DictionaryEntry entry)
            {
                _entry       = entry;
                Format       = Stream.GetAudioFormat().ToLowerInvariant();
                Key          = _entry.Key.ToString();
                DisplayName  = Key.GetTranslation();
                ResourceName = $"{nameof(Properties.Resources)}.{Key}";
            }
        #endregion

        #region Public Properties
            public              string                Key          { get; init; }

            [JsonIgnore] public string                DisplayName  { get; init; }
            [JsonIgnore] public string                Format       { get; init; }
            [JsonIgnore] public string                TempFilePath { get; set; }
            [JsonIgnore] public string                ResourceName { get; init; }

            [JsonIgnore] public UnmanagedMemoryStream Stream => (UnmanagedMemoryStream)_entry.Value;
        #endregion

        public override string ToString()
        {
            return $"{DisplayName} - {Format}";
        }
    }
}
