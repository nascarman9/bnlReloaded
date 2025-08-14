using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchStats
{
    public Dictionary<uint, MatchPlayerStats>? PlayerStats { get; set; }

    public MatchTeamStats? Team1Stats { get; set; }

    public MatchTeamStats? Team2Stats { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(PlayerStats != null, Team1Stats != null, Team2Stats != null).Write(writer);
        if (PlayerStats != null)
            writer.WriteMap(PlayerStats, writer.Write, MatchPlayerStats.WriteRecord);
        if (Team1Stats != null)
            MatchTeamStats.WriteRecord(writer, Team1Stats);
        if (Team2Stats == null)
            return;
        MatchTeamStats.WriteRecord(writer, Team2Stats);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        PlayerStats = bitField[0] ? reader.ReadMap<uint, MatchPlayerStats, Dictionary<uint, MatchPlayerStats>>(reader.ReadUInt32, MatchPlayerStats.ReadRecord) : null;
        Team1Stats = bitField[1] ? MatchTeamStats.ReadRecord(reader) : null;
        Team2Stats = bitField[2] ? MatchTeamStats.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchStats value) => value.Write(writer);

    public static MatchStats ReadRecord(BinaryReader reader)
    {
        var matchStats = new MatchStats();
        matchStats.Read(reader);
        return matchStats;
    }
}