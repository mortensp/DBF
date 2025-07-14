using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DBF
{
    public class DecimalCommaConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return decimal.Parse(value.Replace(",", "."), CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("0.00", new CultureInfo("da-DK")));
        }
    }
}

