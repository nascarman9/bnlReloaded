using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BNLReloadedServer.ProtocolHelpers;

public class QuaternionJsonConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) 
            return default;
        
        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        var json = jsonDocument.RootElement;
        return new Quaternion(json.GetProperty("x").Deserialize<float>(), json.GetProperty("y").Deserialize<float>(),
            json.GetProperty("z").Deserialize<float>(), json.GetProperty("w").Deserialize<float>());
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteNumber("w", value.W);
        writer.WriteEndObject();
    }
}