using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PerkModGear : PerkMod
{
    public override PerkModType Type => PerkModType.Gear;

    public Key ReplaceFrom { get; set; }

    public Key ReplaceTo { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        Key.WriteRecord(writer, ReplaceFrom);
        Key.WriteRecord(writer, ReplaceTo);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            ReplaceFrom = Key.ReadRecord(reader);
        if (!bitField[1])
            return;
        ReplaceTo = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, PerkModGear value) => value.Write(writer);

    public static PerkModGear ReadRecord(BinaryReader reader)
    {
        var perkModGear = new PerkModGear();
        perkModGear.Read(reader);
        return perkModGear;
    }
}