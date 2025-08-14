using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<MapTrigger2>))]
public abstract class MapTrigger2 : IJsonFactory<MapTrigger2>
{
    public abstract MapTriggerType2 Type { get; }

    public string? Tag { get; set; }

    public List<MapTriggerLabel> Labels { get; set; } = [];

    public static MapTrigger2 CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<MapTriggerType2>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, MapTrigger2 value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static MapTrigger2 ReadVariant(BinaryReader reader)
    {
        var mapTrigger2 = Create(reader.ReadByteEnum<MapTriggerType2>());
        mapTrigger2.Read(reader);
        return mapTrigger2;
    }

    public static MapTrigger2 Create(MapTriggerType2 type)
    {
        return type switch
        {
            MapTriggerType2.Box => new MapTriggerBox(),
            MapTriggerType2.Sphere => new MapTriggerSphere(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}