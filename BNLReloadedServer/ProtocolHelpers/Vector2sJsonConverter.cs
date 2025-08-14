using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public class Vector2sJsonConverter : JsonConverter<Vector2s>
{
    public override Vector2s Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) 
            return default;
        
        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        var json = jsonDocument.RootElement;
        return new Vector2s(json.GetProperty("x").Deserialize<short>(), json.GetProperty("y").Deserialize<short>());
    }

    public override void Write(Utf8JsonWriter writer, Vector2s value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.x);
        writer.WriteNumber("y", value.y);
        writer.WriteEndObject();
    }
}