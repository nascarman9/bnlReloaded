using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class WeightedLootItem
{
    public LootItemUnit? Item { get; set; }

    public float Weight { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Item != null, true).Write(writer);
        if (Item != null)
            LootItemUnit.WriteRecord(writer, Item);
        writer.Write(Weight);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Item = bitField[0] ? LootItemUnit.ReadRecord(reader) : null;
        if (!bitField[1])
            return;
        Weight = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, WeightedLootItem value)
    {
        value.Write(writer);
    }

    public static WeightedLootItem ReadRecord(BinaryReader reader)
    {
        var weightedLootItem = new WeightedLootItem();
        weightedLootItem.Read(reader);
        return weightedLootItem;
    }
}