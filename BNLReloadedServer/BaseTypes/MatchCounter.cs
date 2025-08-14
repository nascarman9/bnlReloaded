using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<MatchCounter>))]
public abstract class MatchCounter : IJsonFactory<MatchCounter>
{
    public abstract MatchCounterType Type { get; }

    public static MatchCounter CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<MatchCounterType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, MatchCounter value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static MatchCounter ReadVariant(BinaryReader reader)
    {
        var matchCounter = Create(reader.ReadByteEnum<MatchCounterType>());
        matchCounter.Read(reader);
        return matchCounter;
    }

    public static MatchCounter Create(MatchCounterType type)
    {
        return type switch
        {
            MatchCounterType.Score => new MatchCounterScore(),
            MatchCounterType.DeviceBuilt => new MatchCounterDeviceBuilt(),
            MatchCounterType.UnitKilled => new MatchCounterUnitKilled(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}