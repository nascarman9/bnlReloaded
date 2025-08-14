using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootItemCondition : LootItem
{
    public override LootItemType Type => LootItemType.Condition;

    public Dictionary<LootConditionType, LootItemUnit>? ItemsByCondition { get; set; }

    public Dictionary<LootConditionType, LootItemUnit>? OpponentItemsByCondition { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(ItemsByCondition != null, OpponentItemsByCondition != null).Write(writer);
      if (ItemsByCondition != null)
        writer.WriteMap(ItemsByCondition, writer.WriteByteEnum, LootItemUnit.WriteRecord);
      if (OpponentItemsByCondition == null)
        return;
      writer.WriteMap(OpponentItemsByCondition, writer.WriteByteEnum, LootItemUnit.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      ItemsByCondition = bitField[0] ? reader.ReadMap<LootConditionType, LootItemUnit, Dictionary<LootConditionType, LootItemUnit>>(reader.ReadByteEnum<LootConditionType>, LootItemUnit.ReadRecord) : null;
      OpponentItemsByCondition = bitField[1] ? reader.ReadMap<LootConditionType, LootItemUnit, Dictionary<LootConditionType, LootItemUnit>>(reader.ReadByteEnum<LootConditionType>, LootItemUnit.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, LootItemCondition value)
    {
      value.Write(writer);
    }

    public static LootItemCondition ReadRecord(BinaryReader reader)
    {
      var lootItemCondition = new LootItemCondition();
      lootItemCondition.Read(reader);
      return lootItemCondition;
    }
}