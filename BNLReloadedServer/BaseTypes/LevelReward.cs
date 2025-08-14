using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<LevelReward>))]
public abstract class LevelReward : IJsonFactory<LevelReward>
{
    public abstract LevelRewardType Type { get; }

    public static LevelReward CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<LevelRewardType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, LevelReward value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static LevelReward ReadVariant(BinaryReader reader)
    {
        var levelReward = Create(reader.ReadByteEnum<LevelRewardType>());
        levelReward.Read(reader);
        return levelReward;
    }

    public static LevelReward Create(LevelRewardType type)
    {
        return type switch
        {
            LevelRewardType.Currency => new LevelRewardCurrency(),
            LevelRewardType.LootCrate => new LevelRewardLootCrate(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}