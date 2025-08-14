using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneEventTurretFire : ZoneEvent
{
    public override ZoneEventType Type => ZoneEventType.TurretFire;

    public uint TurretId { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(TurretId);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        TurretId = reader.ReadUInt32();
    }

    public static void WriteRecord(BinaryWriter writer, ZoneEventTurretFire value)
    {
        value.Write(writer);
    }

    public static ZoneEventTurretFire ReadRecord(BinaryReader reader)
    {
        var zoneEventTurretFire = new ZoneEventTurretFire();
        zoneEventTurretFire.Read(reader);
        return zoneEventTurretFire;
    }
}