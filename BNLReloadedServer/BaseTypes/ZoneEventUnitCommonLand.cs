using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneEventUnitCommonLand : ZoneEvent
{
    public override ZoneEventType Type => ZoneEventType.UnitCommonLand;

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

    public static void WriteRecord(BinaryWriter writer, ZoneEventUnitCommonLand value)
    {
        value.Write(writer);
    }

    public static ZoneEventUnitCommonLand ReadRecord(BinaryReader reader)
    {
        var eventUnitCommonLand = new ZoneEventUnitCommonLand();
        eventUnitCommonLand.Read(reader);
        return eventUnitCommonLand;
    }
}