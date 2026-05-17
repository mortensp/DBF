using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.Json;
using System.Text.Json.Serialization;
using String.Localization;

namespace DBF.Converters
{
    /// <summary>
    /// Convert Preset Names from Json, of they are localized strings rater than keys.
    /// </summary>
    public class PresetNameJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonValue = reader.GetString();

            if (string.IsNullOrEmpty(jsonValue))
                return jsonValue;

            // Try to map from translated text to resource key
            var resourceKey = ResolveKeyFromTranslatedValue(jsonValue);
            return resourceKey ?? jsonValue;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            // Write the resource key directly (not the translated text)
            writer.WriteStringValue(value);
        }

        private static string ResolveKeyFromTranslatedValue(string translatedValue)
        {
            if (string.IsNullOrEmpty(translatedValue))
                return null;

            var rmProp = typeof(Lex).GetProperty("ResourceManager", BindingFlags.NonPublic | BindingFlags.Static);

            if (rmProp == null)
                return null;

            try
            {
                var rm = (ResourceManager)rmProp.GetValue(null);

                // Search for all available cultures
                var cultures = LanguageService.Instance?.GetAvailableCultures()
                            ?? CultureInfo.GetCultures(CultureTypes.SpecificCultures);

                foreach (var culture in cultures)
                    try
                    {
                        var rs = rm.GetResourceSet(culture, true, false);

                        if (rs == null)
                            continue;

                        foreach (DictionaryEntry entry in rs)
                        {
                            if (entry.Value is string s && string.Equals(s, translatedValue, StringComparison.Ordinal))
                                return entry.Key as string;
                        }
                    }

                    catch { }
            }

            catch { }

            return null;
        }
    }
}
