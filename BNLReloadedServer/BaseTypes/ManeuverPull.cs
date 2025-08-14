using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ManeuverPull : Maneuver
{
    public override ManeuverType Type => ManeuverType.Pull;

    public Vector3? OriginPos { get; set; }

    public uint? OriginUnitId { get; set; }

    public float? Force { get; set; }

    public bool Enabled { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(OriginPos.HasValue, OriginUnitId.HasValue, Force.HasValue, true).Write(writer);
        if (OriginPos.HasValue)
            writer.Write(OriginPos.Value);
        if (OriginUnitId.HasValue)
            writer.Write(OriginUnitId.Value);
        if (Force.HasValue)
            writer.Write(Force.Value);
        writer.Write(Enabled);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        OriginPos = bitField[0] ? reader.ReadVector3() : null;
        OriginUnitId = bitField[1] ? reader.ReadUInt32() : null;
        Force = bitField[2] ? reader.ReadSingle() : null;
        if (!bitField[3])
            return;
        Enabled = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, ManeuverPull value) => value.Write(writer);

    public static ManeuverPull ReadRecord(BinaryReader reader)
    {
        var maneuverPull = new ManeuverPull();
        maneuverPull.Read(reader);
        return maneuverPull;
    }
}