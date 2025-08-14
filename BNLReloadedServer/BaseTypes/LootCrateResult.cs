using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<Card>))]
public abstract class LootCrateResult : IJsonFactory<LootCrateResult>
{
    public abstract LootEntryType Type { get; }

    public static LootCrateResult CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<LootEntryType>());
    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, LootCrateResult value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static LootCrateResult ReadVariant(BinaryReader reader)
    {
        var lootCrateResult = Create(reader.ReadByteEnum<LootEntryType>());
        lootCrateResult.Read(reader);
        return lootCrateResult;
    }

    public static LootCrateResult Create(LootEntryType type)
    {
        return type switch
        {
            LootEntryType.Rubble => new LootCrateResultRubble(),
            LootEntryType.Gold => new LootCrateResultGold(),
            LootEntryType.Item => new LootCrateResultItem(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}