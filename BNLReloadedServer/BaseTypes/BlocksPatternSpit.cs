using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlocksPatternSpit : BlocksPattern
{
    public override BlocksPatternType Type => BlocksPatternType.Spit;

    public Key BlockKey { get; set; }

    public float Radius { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        Key.WriteRecord(writer, BlockKey);
        writer.Write(Radius);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            BlockKey = Key.ReadRecord(reader);
        if (!bitField[1])
            return;
        Radius = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, BlocksPatternSpit value)
    {
        value.Write(writer);
    }

    public static BlocksPatternSpit ReadRecord(BinaryReader reader)
    {
        var blocksPatternSpit = new BlocksPatternSpit();
        blocksPatternSpit.Read(reader);
        return blocksPatternSpit;
    }
}