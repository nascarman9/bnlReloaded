using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<MatchData>))]
public abstract class MatchData : IJsonFactory<MatchData>
{
    public abstract MatchType Type { get; }

    public static MatchData CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<MatchType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, MatchData value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static MatchData ReadVariant(BinaryReader reader)
    {
        var matchData = Create(reader.ReadByteEnum<MatchType>());
        matchData.Read(reader);
        return matchData;
    }

    public static MatchData Create(MatchType type)
    {
        return type switch
        {
            MatchType.ShieldRush2 => new MatchDataShieldRush2(),
            MatchType.ShieldCapture => new MatchDataShieldCapture(),
            MatchType.Tutorial => new MatchDataTutorial(),
            MatchType.TimeTrial => new MatchDataTimeTrial(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}