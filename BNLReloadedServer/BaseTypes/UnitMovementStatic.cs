using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitMovementStatic : UnitMovement
{
    public override UnitMovementType Type => UnitMovementType.Static;

    public override void Write(BinaryWriter writer) => new BitField().Write(writer);

    public override void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, UnitMovementStatic value)
    {
        value.Write(writer);
    }

    public static UnitMovementStatic ReadRecord(BinaryReader reader)
    {
        var unitMovementStatic = new UnitMovementStatic();
        unitMovementStatic.Read(reader);
        return unitMovementStatic;
    }
}