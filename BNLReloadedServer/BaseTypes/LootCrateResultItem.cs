using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootCrateResultItem : LootCrateResult
{
    public override LootEntryType Type => LootEntryType.Item;

    public Key Key { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        Key.WriteRecord(writer, Key);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        Key = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, LootCrateResultItem value)
    {
        value.Write(writer);
    }

    public static LootCrateResultItem ReadRecord(BinaryReader reader)
    {
        var lootCrateResultItem = new LootCrateResultItem();
        lootCrateResultItem.Read(reader);
        return lootCrateResultItem;
    }
}