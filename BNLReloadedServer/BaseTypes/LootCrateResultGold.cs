using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootCrateResultGold : LootCrateResult
{
    public override LootEntryType Type => LootEntryType.Gold;

    public int Amount { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(Amount);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        Amount = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, LootCrateResultGold value)
    {
        value.Write(writer);
    }

    public static LootCrateResultGold ReadRecord(BinaryReader reader)
    {
        var lootCrateResultGold = new LootCrateResultGold();
        lootCrateResultGold.Read(reader);
        return lootCrateResultGold;
    }
}