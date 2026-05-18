//using NAudio.Wave;
using System.Linq;
using System.Text.Json.Serialization;
using String.Localization;

namespace DBF.AudioServices
{
    public class SoundDefinition
    {
        public SoundDefinition()
        {
        }

        public SoundDefinition(string displayName)
        {
            Key = LanguageService.GetKeyForTranslation(typeof(Lex), displayName) ?? displayName;
        }

        public string Key          { get; set; }          // invariant
        [JsonIgnore] public string DisplayName => LanguageService.GetTranslationFor(typeof(Lex), Key);
        [JsonIgnore] public string ResourceName => $"DBF.Resources.Sounds.{Key}.wav";
    }
}
