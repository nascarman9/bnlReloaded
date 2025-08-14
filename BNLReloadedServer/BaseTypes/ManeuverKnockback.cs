using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ManeuverKnockback : Maneuver
{
    public override ManeuverType Type => ManeuverType.Knockback;

    public Vector3 Origin { get; set; }

    public float Force { get; set; }

    public float MidairForce { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.Write(Origin);
        writer.Write(Force);
        writer.Write(MidairForce);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            Origin = reader.ReadVector3();
        if (bitField[1])
            Force = reader.ReadSingle();
        if (!bitField[2])
            return;
        MidairForce = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ManeuverKnockback value)
    {
        value.Write(writer);
    }

    public static ManeuverKnockback ReadRecord(BinaryReader reader)
    {
        var maneuverKnockback = new ManeuverKnockback();
        maneuverKnockback.Read(reader);
        return maneuverKnockback;
    }
}