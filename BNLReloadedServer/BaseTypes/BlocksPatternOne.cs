using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlocksPatternOne : BlocksPattern
{
    public override BlocksPatternType Type => BlocksPatternType.One;

    public Key BlockKey { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        Key.WriteRecord(writer, BlockKey);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        BlockKey = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, BlocksPatternOne value)
    {
        value.Write(writer);
    }

    public static BlocksPatternOne ReadRecord(BinaryReader reader)
    {
        var blocksPatternOne = new BlocksPatternOne();
        blocksPatternOne.Read(reader);
        return blocksPatternOne;
    }
}