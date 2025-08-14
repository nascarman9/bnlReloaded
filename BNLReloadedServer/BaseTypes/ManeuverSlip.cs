using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ManeuverSlip : Maneuver
{
    public override ManeuverType Type => ManeuverType.Slip;

    public float DirectionAngle { get; set; }

    public float Distance { get; set; }

    public float Time { get; set; }

    public float RotationTime { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true).Write(writer);
        writer.Write(DirectionAngle);
        writer.Write(Distance);
        writer.Write(Time);
        writer.Write(RotationTime);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        if (bitField[0])
            DirectionAngle = reader.ReadSingle();
        if (bitField[1])
            Distance = reader.ReadSingle();
        if (bitField[2])
            Time = reader.ReadSingle();
        if (!bitField[3])
            return;
        RotationTime = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ManeuverSlip value) => value.Write(writer);

    public static ManeuverSlip ReadRecord(BinaryReader reader)
    {
        var maneuverSlip = new ManeuverSlip();
        maneuverSlip.Read(reader);
        return maneuverSlip;
    }
}