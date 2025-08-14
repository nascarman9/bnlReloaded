using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneObjective
{
    public TeamType Team { get; set; }

    public int Id { get; set; }

    public int Counter { get; set; }

    public int RequiredCounter { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true).Write(writer);
        writer.WriteByteEnum(Team);
        writer.Write(Id);
        writer.Write(Counter);
        writer.Write(RequiredCounter);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        if (bitField[0])
            Team = reader.ReadByteEnum<TeamType>();
        if (bitField[1])
            Id = reader.ReadInt32();
        if (bitField[2])
            Counter = reader.ReadInt32();
        if (!bitField[3])
            return;
        RequiredCounter = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, ZoneObjective value) => value.Write(writer);

    public static ZoneObjective ReadRecord(BinaryReader reader)
    {
        var zoneObjective = new ZoneObjective();
        zoneObjective.Read(reader);
        return zoneObjective;
    }
}