using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlockSpecialBounce : BlockSpecial
{
    public override BlockSpecialType Type => BlockSpecialType.Bounce;

    public float BounceForce { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(BounceForce);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        BounceForce = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, BlockSpecialBounce value)
    {
        value.Write(writer);
    }

    public static BlockSpecialBounce ReadRecord(BinaryReader reader)
    {
        var blockSpecialBounce = new BlockSpecialBounce();
        blockSpecialBounce.Read(reader);
        return blockSpecialBounce;
    }
}