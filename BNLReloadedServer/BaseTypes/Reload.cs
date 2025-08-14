using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<Reload>))]
public abstract class Reload : IJsonFactory<Reload>
{
    public abstract ReloadType Type { get; }

    public static Reload CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<ReloadType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, Reload value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static Reload ReadVariant(BinaryReader reader)
    {
        var reload = Create(reader.ReadByteEnum<ReloadType>());
        reload.Read(reader);
        return reload;
    }

    public static Reload Create(ReloadType type)
    {
        return type switch
        {
            ReloadType.FullClip => new ReloadFullClip(),
            ReloadType.Partial => new ReloadPartial(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}