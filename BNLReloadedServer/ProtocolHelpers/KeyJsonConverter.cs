using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;

namespace BNLReloadedServer.ProtocolHelpers;

public class KeyJsonConverter : JsonConverter<Key>
{
    public override Key Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Catalogue.Key(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Key value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Databases.Catalogue.GetCard<Card>(value)?.Id);
    }

    public override Key ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, Key value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(Databases.Catalogue.GetCard<Card>(value)!.Id);
    }
}