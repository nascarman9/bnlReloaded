using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlockSpecialExternal : BlockSpecial
{
    public override BlockSpecialType Type => BlockSpecialType.External;

    public override void Write(BinaryWriter writer) => new BitField().Write(writer);

    public override void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, BlockSpecialExternal value)
    {
        value.Write(writer);
    }

    public static BlockSpecialExternal ReadRecord(BinaryReader reader)
    {
        var blockSpecialExternal = new BlockSpecialExternal();
        blockSpecialExternal.Read(reader);
        return blockSpecialExternal;
    }
}