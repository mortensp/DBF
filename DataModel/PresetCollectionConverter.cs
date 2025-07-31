using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Caliburn.Micro;
using Syncfusion.Data.Extensions;

namespace DBF.DataModel
{
    public class PresetCollectionConverter : JsonConverter<BindableCollection<Preset>>
    {
        public override BindableCollection<Preset> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var presets = JsonSerializer.Deserialize<List<Preset>>(ref reader, options);
            return new BindableCollection<Preset>(presets ?? []);
        }

        public override void Write(Utf8JsonWriter writer, BindableCollection<Preset> value, JsonSerializerOptions options)
        {
            var filtered = value.Where(p => p.CustomPreset).ToList();
            JsonSerializer.Serialize(writer, filtered, options);
        }
    }
}

