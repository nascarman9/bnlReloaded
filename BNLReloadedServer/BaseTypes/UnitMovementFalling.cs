using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitMovementFalling : UnitMovement
{
    public override UnitMovementType Type => UnitMovementType.Falling;

    public float StartSpeed { get; set; }

    public float Gravity { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(StartSpeed);
        writer.Write(Gravity);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            StartSpeed = reader.ReadSingle();
        if (!bitField[1])
            return;
        Gravity = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, UnitMovementFalling value)
    {
        value.Write(writer);
    }

    public static UnitMovementFalling ReadRecord(BinaryReader reader)
    {
        var unitMovementFalling = new UnitMovementFalling();
        unitMovementFalling.Read(reader);
        return unitMovementFalling;
    }
}