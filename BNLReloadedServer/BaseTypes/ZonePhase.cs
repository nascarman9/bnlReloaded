using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZonePhase
{
    public ZonePhaseType PhaseType { get; set; }

    public long StartTime { get; set; }

    public long? EndTime { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, EndTime.HasValue).Write(writer);
        writer.WriteByteEnum(PhaseType);
        writer.Write(StartTime);
        if (!EndTime.HasValue)
            return;
        writer.Write(EndTime.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            PhaseType = reader.ReadByteEnum<ZonePhaseType>();
        if (bitField[1])
            StartTime = reader.ReadInt64();
        EndTime = bitField[2] ? reader.ReadInt64() : null;
    }

    public static void WriteRecord(BinaryWriter writer, ZonePhase value) => value.Write(writer);

    public static ZonePhase ReadRecord(BinaryReader reader)
    {
        var zonePhase = new ZonePhase();
        zonePhase.Read(reader);
        return zonePhase;
    }
}