using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootItemRandom : LootItem
{
    public override LootItemType Type => LootItemType.Random;

    public List<WeightedLootItem>? ItemsByWeight { get; set; }

    public List<WeightedLootItem>? OpponentItemsByWeight { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(ItemsByWeight != null, OpponentItemsByWeight != null).Write(writer);
      if (ItemsByWeight != null)
        writer.WriteList(ItemsByWeight, WeightedLootItem.WriteRecord);
      if (OpponentItemsByWeight == null)
        return;
      writer.WriteList(OpponentItemsByWeight, WeightedLootItem.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      ItemsByWeight = bitField[0] ? reader.ReadList<WeightedLootItem, List<WeightedLootItem>>(WeightedLootItem.ReadRecord) : null;
      OpponentItemsByWeight = bitField[1] ? reader.ReadList<WeightedLootItem, List<WeightedLootItem>>(WeightedLootItem.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, LootItemRandom value)
    {
      value.Write(writer);
    }

    public static LootItemRandom ReadRecord(BinaryReader reader)
    {
      var lootItemRandom = new LootItemRandom();
      lootItemRandom.Read(reader);
      return lootItemRandom;
    }
}