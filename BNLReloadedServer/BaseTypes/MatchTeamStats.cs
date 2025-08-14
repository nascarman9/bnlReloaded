using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchTeamStats
{
    public int Warfare { get; set; }

    public int Construction { get; set; }

    public int Tactics { get; set; }

    public int Healing { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true).Write(writer);
        writer.Write(Warfare);
        writer.Write(Construction);
        writer.Write(Tactics);
        writer.Write(Healing);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        if (bitField[0])
            Warfare = reader.ReadInt32();
        if (bitField[1])
            Construction = reader.ReadInt32();
        if (bitField[2])
            Tactics = reader.ReadInt32();
        if (!bitField[3])
            return;
        Healing = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, MatchTeamStats value)
    {
        value.Write(writer);
    }

    public static MatchTeamStats ReadRecord(BinaryReader reader)
    {
        var matchTeamStats = new MatchTeamStats();
        matchTeamStats.Read(reader);
        return matchTeamStats;
    }
}