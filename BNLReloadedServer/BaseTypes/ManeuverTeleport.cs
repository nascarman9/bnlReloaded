using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ManeuverTeleport : Maneuver
{
    public override ManeuverType Type => ManeuverType.Teleport;

    public Vector3 Position { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(Position);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        Position = reader.ReadVector3();
    }

    public static void WriteRecord(BinaryWriter writer, ManeuverTeleport value)
    {
        value.Write(writer);
    }

    public static ManeuverTeleport ReadRecord(BinaryReader reader)
    {
        var maneuverTeleport = new ManeuverTeleport();
        maneuverTeleport.Read(reader);
        return maneuverTeleport;
    }
}