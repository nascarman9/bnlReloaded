using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneEventToolHold : ZoneEvent
{
    public override ZoneEventType Type => ZoneEventType.ToolHold;

    public uint UnitId { get; set; }

    public byte ToolIndex { get; set; }

    public bool Active { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.Write(UnitId);
        writer.Write(ToolIndex);
        writer.Write(Active);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            UnitId = reader.ReadUInt32();
        if (bitField[1])
            ToolIndex = reader.ReadByte();
        if (!bitField[2])
            return;
        Active = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, ZoneEventToolHold value)
    {
        value.Write(writer);
    }

    public static ZoneEventToolHold ReadRecord(BinaryReader reader)
    {
        var zoneEventToolHold = new ZoneEventToolHold();
        zoneEventToolHold.Read(reader);
        return zoneEventToolHold;
    }
}