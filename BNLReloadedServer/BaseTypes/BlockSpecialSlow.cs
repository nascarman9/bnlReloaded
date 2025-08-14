using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlockSpecialSlow : BlockSpecial
{
    public override BlockSpecialType Type => BlockSpecialType.Slow;

    public float SpeedModifier { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(SpeedModifier);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        SpeedModifier = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, BlockSpecialSlow value)
    {
        value.Write(writer);
    }

    public static BlockSpecialSlow ReadRecord(BinaryReader reader)
    {
        var blockSpecialSlow = new BlockSpecialSlow();
        blockSpecialSlow.Read(reader);
        return blockSpecialSlow;
    }
}