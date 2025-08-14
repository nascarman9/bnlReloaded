using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public class Vector3sJsonConverter : JsonConverter<Vector3s>
{
    public override Vector3s Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) 
            return default;
        
        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        var json = jsonDocument.RootElement;
        return new Vector3s(json.GetProperty("x").Deserialize<short>(), json.GetProperty("y").Deserialize<short>(), json.GetProperty("z").Deserialize<short>());
    }

    public override void Write(Utf8JsonWriter writer, Vector3s value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.x);
        writer.WriteNumber("y", value.y);
        writer.WriteNumber("z", value.z);
        writer.WriteEndObject();
    }
}