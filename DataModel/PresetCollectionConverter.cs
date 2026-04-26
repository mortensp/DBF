using System.Text.Json;
using System.Text.Json.Serialization;
using Caliburn.Micro;
using DBF.Helpers;

namespace DBF.DataModel
{
    public class PresetCollectionConverter : JsonConverter<BindableCollectionExt<Preset>>
    {
        public override BindableCollectionExt<Preset> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var presets = JsonSerializer.Deserialize<List<Preset>>(ref reader, options);
            return new BindableCollectionExt<Preset>(presets ?? []);
        }

        public override void Write(Utf8JsonWriter writer, BindableCollectionExt<Preset> value, JsonSerializerOptions options)
        {
            var filtered = value.Where(p => p.CustomPreset).ToList();
            JsonSerializer.Serialize(writer, filtered, options);
        }
    }
}

