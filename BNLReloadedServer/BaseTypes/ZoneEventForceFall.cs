using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneEventForceFall : ZoneEvent
{
    public override ZoneEventType Type => ZoneEventType.ForceFall;

    public uint UnitId { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(UnitId);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        UnitId = reader.ReadUInt32();
    }

    public static void WriteRecord(BinaryWriter writer, ZoneEventForceFall value)
    {
        value.Write(writer);
    }

    public static ZoneEventForceFall ReadRecord(BinaryReader reader)
    {
        var zoneEventForceFall = new ZoneEventForceFall();
        zoneEventForceFall.Read(reader);
        return zoneEventForceFall;
    }
}