using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public abstract class Maneuver
{
    public abstract ManeuverType Type { get; }

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, Maneuver value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static Maneuver ReadVariant(BinaryReader reader)
    {
        var maneuver = Create(reader.ReadByteEnum<ManeuverType>());
        maneuver.Read(reader);
        return maneuver;
    }

    public static Maneuver Create(ManeuverType type)
    {
        return type switch
        {
            ManeuverType.Teleport => new ManeuverTeleport(),
            ManeuverType.Knockback => new ManeuverKnockback(),
            ManeuverType.Pull => new ManeuverPull(),
            ManeuverType.Slip => new ManeuverSlip(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}