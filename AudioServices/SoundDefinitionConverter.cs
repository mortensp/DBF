using System.Text.Json;
using System.Text.Json.Serialization;

namespace DBF.AudioServices
{
    public class SoundDefinitionConverter : JsonConverter<SoundDefinition>
    {
        public override SoundDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object");

            string key = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();

                    if (propertyName == "Key")
                        key = reader.GetString();
                }
            }

            // Create SoundDefinition with the key value
            if (!string.IsNullOrWhiteSpace(key))
                return new SoundDefinition(key);

            return null;
        }

        public override void Write(Utf8JsonWriter writer, SoundDefinition value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteString("Key", value.Key);
            writer.WriteEndObject();
        }
    }
}
