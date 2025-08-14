using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<MatchObjective>))]
public abstract class MatchObjective : IJsonFactory<MatchObjective>
{
    public abstract MatchObjectiveType Type { get; }

    public int Id { get; set; }

    public TeamType Team { get; set; }

    public LocalizedString? Description { get; set; }

    public static MatchObjective CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<MatchObjectiveType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, MatchObjective value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static MatchObjective ReadVariant(BinaryReader reader)
    {
        var matchObjective = Create(reader.ReadByteEnum<MatchObjectiveType>());
        matchObjective.Read(reader);
        return matchObjective;
    }

    public static MatchObjective Create(MatchObjectiveType type)
    {
        return type switch
        {
            MatchObjectiveType.KillUnits => new MatchObjectiveKillUnits(),
            MatchObjectiveType.CollectPickups => new MatchObjectiveCollectPickups(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}