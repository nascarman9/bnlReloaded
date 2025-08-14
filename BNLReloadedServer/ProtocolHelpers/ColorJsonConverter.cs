using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BNLReloadedServer.ProtocolHelpers;

public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) 
            return default;
        
        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        var json = jsonDocument.RootElement;
        return Color.FromArgb(
            json.GetProperty("a").Deserialize<byte>(), 
            json.GetProperty("r").Deserialize<byte>(),
            json.GetProperty("g").Deserialize<byte>(),
            json.GetProperty("b").Deserialize<byte>());
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("r", value.R);
        writer.WriteNumber("g", value.G);
        writer.WriteNumber("b", value.B);
        writer.WriteNumber("a", value.A);
        writer.WriteEndObject();
    }
}