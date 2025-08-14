using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitMovementCustom : UnitMovement
{
    public override UnitMovementType Type => UnitMovementType.Custom;

    public override void Write(BinaryWriter writer) => new BitField().Write(writer);

    public override void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, UnitMovementCustom value)
    {
        value.Write(writer);
    }

    public static UnitMovementCustom ReadRecord(BinaryReader reader)
    {
        var unitMovementCustom = new UnitMovementCustom();
        unitMovementCustom.Read(reader);
        return unitMovementCustom;
    }
}