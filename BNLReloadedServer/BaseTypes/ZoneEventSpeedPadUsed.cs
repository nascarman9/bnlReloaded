using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneEventSpeedPadUsed : ZoneEvent
{
    public override ZoneEventType Type => ZoneEventType.SpeedPadUsed;

    public uint UnitId { get; set; }

    public Vector3 Position { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(UnitId);
        writer.Write(Position);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            UnitId = reader.ReadUInt32();
        if (!bitField[1])
            return;
        Position = reader.ReadVector3();
    }

    public static void WriteRecord(BinaryWriter writer, ZoneEventSpeedPadUsed value)
    {
        value.Write(writer);
    }

    public static ZoneEventSpeedPadUsed ReadRecord(BinaryReader reader)
    {
        var eventSpeedPadUsed = new ZoneEventSpeedPadUsed();
        eventSpeedPadUsed.Read(reader);
        return eventSpeedPadUsed;
    }
}