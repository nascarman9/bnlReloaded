using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class League
{
    public int Tier { get; set; }

    public int Division { get; set; }

    public int Points { get; set; }

    public DateTime JoinedTime { get; set; }

    public DateTime LastPlayedTime { get; set; }

    public int? Status { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true, true, Status.HasValue).Write(writer);
        writer.Write(Tier);
        writer.Write(Division);
        writer.Write(Points);
        writer.WriteDateTime(JoinedTime);
        writer.WriteDateTime(LastPlayedTime);
        if (!Status.HasValue)
            return;
        writer.Write(Status.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(6);
        bitField.Read(reader);
        if (bitField[0])
            Tier = reader.ReadInt32();
        if (bitField[1])
            Division = reader.ReadInt32();
        if (bitField[2])
            Points = reader.ReadInt32();
        if (bitField[3])
            JoinedTime = reader.ReadDateTime();
        if (bitField[4])
            LastPlayedTime = reader.ReadDateTime();
        Status = bitField[5] ? reader.ReadInt32() : null;
    }

    public static void WriteRecord(BinaryWriter writer, League value) => value.Write(writer);

    public static League ReadRecord(BinaryReader reader)
    {
        var league = new League();
        league.Read(reader);
        return league;
    }
}