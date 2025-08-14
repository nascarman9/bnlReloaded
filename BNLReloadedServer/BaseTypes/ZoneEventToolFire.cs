using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneEventToolFire : ZoneEvent
{
    public override ZoneEventType Type => ZoneEventType.ToolFire;

    public uint UnitId { get; set; }

    public byte ToolIndex { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(UnitId);
        writer.Write(ToolIndex);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            UnitId = reader.ReadUInt32();
        if (!bitField[1])
            return;
        ToolIndex = reader.ReadByte();
    }

    public static void WriteRecord(BinaryWriter writer, ZoneEventToolFire value)
    {
        value.Write(writer);
    }

    public static ZoneEventToolFire ReadRecord(BinaryReader reader)
    {
        var zoneEventToolFire = new ZoneEventToolFire();
        zoneEventToolFire.Read(reader);
        return zoneEventToolFire;
    }
}