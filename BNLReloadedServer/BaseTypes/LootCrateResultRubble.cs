using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootCrateResultRubble : LootCrateResult
{
    public override LootEntryType Type => LootEntryType.Rubble;

    public Key RubbleKey { get; set; }

    public int Amount { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        Key.WriteRecord(writer, RubbleKey);
        writer.Write(Amount);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            RubbleKey = Key.ReadRecord(reader);
        if (!bitField[1])
            return;
        Amount = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, LootCrateResultRubble value)
    {
        value.Write(writer);
    }

    public static LootCrateResultRubble ReadRecord(BinaryReader reader)
    {
        var crateResultRubble = new LootCrateResultRubble();
        crateResultRubble.Read(reader);
        return crateResultRubble;
    }
}