using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootEntryGold : LootEntry
{
    public override LootEntryType Type => LootEntryType.Gold;

    public int Min { get; set; } = 1;

    public int Max { get; set; } = 1;

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.Write(Weight);
        writer.Write(Min);
        writer.Write(Max);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            Weight = reader.ReadSingle();
        if (bitField[1])
            Min = reader.ReadInt32();
        if (!bitField[2])
            return;
        Max = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, LootEntryGold value) => value.Write(writer);

    public static LootEntryGold ReadRecord(BinaryReader reader)
    {
        var lootEntryGold = new LootEntryGold();
        lootEntryGold.Read(reader);
        return lootEntryGold;
    }
}