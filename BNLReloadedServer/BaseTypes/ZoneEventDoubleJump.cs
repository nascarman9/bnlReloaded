using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneEventDoubleJump : ZoneEvent
{
    public override ZoneEventType Type => ZoneEventType.DoubleJump;

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

    public static void WriteRecord(BinaryWriter writer, ZoneEventDoubleJump value)
    {
        value.Write(writer);
    }

    public static ZoneEventDoubleJump ReadRecord(BinaryReader reader)
    {
        var zoneEventDoubleJump = new ZoneEventDoubleJump();
        zoneEventDoubleJump.Read(reader);
        return zoneEventDoubleJump;
    }
}