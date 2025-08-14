using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlockSpecialNoFallDamage : BlockSpecial
{
    public override BlockSpecialType Type => BlockSpecialType.NoFallDamage;

    public override void Write(BinaryWriter writer) => new BitField().Write(writer);

    public override void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, BlockSpecialNoFallDamage value)
    {
        value.Write(writer);
    }

    public static BlockSpecialNoFallDamage ReadRecord(BinaryReader reader)
    {
        var specialNoFallDamage = new BlockSpecialNoFallDamage();
        specialNoFallDamage.Read(reader);
        return specialNoFallDamage;
    }
}