using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<LootItem>))]
public abstract class LootItem : IJsonFactory<LootItem>
{
    public abstract LootItemType Type { get; }

    public static LootItem CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<LootItemType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, LootItem value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static LootItem ReadVariant(BinaryReader reader)
    {
        var lootItem = Create(reader.ReadByteEnum<LootItemType>());
        lootItem.Read(reader);
        return lootItem;
    }

    public static LootItem Create(LootItemType type)
    {
        return type switch
        {
            LootItemType.Common => new LootItemCommon(),
            LootItemType.Random => new LootItemRandom(),
            LootItemType.Condition => new LootItemCondition(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}