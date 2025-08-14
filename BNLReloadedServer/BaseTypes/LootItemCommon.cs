using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootItemCommon : LootItem
{
    public override LootItemType Type => LootItemType.Common;

    public LootItemUnit? Item { get; set; }

    public LootItemUnit? OpponentItem { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Item != null, OpponentItem != null).Write(writer);
      if (Item != null)
        LootItemUnit.WriteRecord(writer, Item);
      if (OpponentItem == null)
        return;
      LootItemUnit.WriteRecord(writer, OpponentItem);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      Item = bitField[0] ? LootItemUnit.ReadRecord(reader) : null;
      OpponentItem = bitField[1] ? LootItemUnit.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, LootItemCommon value)
    {
      value.Write(writer);
    }

    public static LootItemCommon ReadRecord(BinaryReader reader)
    {
      var lootItemCommon = new LootItemCommon();
      lootItemCommon.Read(reader);
      return lootItemCommon;
    }
}