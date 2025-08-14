using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootEntryItem : LootEntry
{
    public override LootEntryType Type => LootEntryType.Item;

    public List<Key>? Keys { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, Keys != null).Write(writer);
        writer.Write(Weight);
        if (Keys != null)
            writer.WriteList(Keys, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            Weight = reader.ReadSingle();
        Keys = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, LootEntryItem value) => value.Write(writer);

    public static LootEntryItem ReadRecord(BinaryReader reader)
    {
        var lootEntryItem = new LootEntryItem();
        lootEntryItem.Read(reader);
        return lootEntryItem;
    }
}