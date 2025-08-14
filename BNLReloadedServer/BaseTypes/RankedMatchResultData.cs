using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class RankedMatchResultData
{
    public int LeagueTierOld { get; set; }

    public int LeagueTierNew { get; set; }

    public int LeagueDivisionOld { get; set; }

    public int LeagueDivisionNew { get; set; }

    public int LeaguePointsOld { get; set; }

    public int LeaguePointsNew { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true, true, true).Write(writer);
        writer.Write(LeagueTierOld);
        writer.Write(LeagueTierNew);
        writer.Write(LeagueDivisionOld);
        writer.Write(LeagueDivisionNew);
        writer.Write(LeaguePointsOld);
        writer.Write(LeaguePointsNew);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(6);
        bitField.Read(reader);
        if (bitField[0])
            LeagueTierOld = reader.ReadInt32();
        if (bitField[1])
            LeagueTierNew = reader.ReadInt32();
        if (bitField[2])
            LeagueDivisionOld = reader.ReadInt32();
        if (bitField[3])
            LeagueDivisionNew = reader.ReadInt32();
        if (bitField[4])
            LeaguePointsOld = reader.ReadInt32();
        if (!bitField[5])
            return;
        LeaguePointsNew = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, RankedMatchResultData value)
    {
        value.Write(writer);
    }

    public static RankedMatchResultData ReadRecord(BinaryReader reader)
    {
        var rankedMatchResultData = new RankedMatchResultData();
        rankedMatchResultData.Read(reader);
        return rankedMatchResultData;
    }
}