using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public class ColorFloatJsonConverter : JsonConverter<ColorFloat>
{
    public override ColorFloat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        var json = jsonDocument.RootElement;
        return new ColorFloat(
            json.GetProperty("r").Deserialize<float>(),
            json.GetProperty("g").Deserialize<float>(),
            json.GetProperty("b").Deserialize<float>(),
            json.GetProperty("a").Deserialize<float>());
    }

    public override void Write(Utf8JsonWriter writer, ColorFloat value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("r", value.r);
        writer.WriteNumber("g", value.g);
        writer.WriteNumber("b", value.b);
        writer.WriteNumber("a", value.a);
        writer.WriteEndObject();
    }
}