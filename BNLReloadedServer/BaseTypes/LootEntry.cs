using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<LootEntry>))]
public abstract class LootEntry : IJsonFactory<LootEntry>
{
    public abstract LootEntryType Type { get; }

    public float Weight { get; set; } = 1f;

    public static LootEntry CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<LootEntryType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, LootEntry value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static LootEntry ReadVariant(BinaryReader reader)
    {
        LootEntry lootEntry = Create(reader.ReadByteEnum<LootEntryType>());
        lootEntry.Read(reader);
        return lootEntry;
    }

    public static LootEntry Create(LootEntryType type)
    {
        return type switch
        {
            LootEntryType.Rubble => new LootEntryRubble(),
            LootEntryType.Gold => new LootEntryGold(),
            LootEntryType.Item => new LootEntryItem(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}