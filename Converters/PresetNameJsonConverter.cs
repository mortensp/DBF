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
            var resourceKey = LanguageService.GetKeyForTranslation(typeof(Lex), jsonValue);
            return resourceKey ?? jsonValue;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            // Write the resource key directly (not the translated text)
            writer.WriteStringValue(value);
        }
    }
}
