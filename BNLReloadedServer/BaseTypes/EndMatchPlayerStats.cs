using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class EndMatchPlayerStats
{
    public Dictionary<PlayerMatchStatType, int>? Stats { get; set; }

    public int Total { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Stats != null, true).Write(writer);
        if (Stats != null)
            writer.WriteMap(Stats, writer.WriteByteEnum, writer.Write);
        writer.Write(Total);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Stats = bitField[0] ? reader.ReadMap<PlayerMatchStatType, int, Dictionary<PlayerMatchStatType, int>>(reader.ReadByteEnum<PlayerMatchStatType>, reader.ReadInt32) : null;
        if (!bitField[1])
            return;
        Total = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, EndMatchPlayerStats value)
    {
        value.Write(writer);
    }

    public static EndMatchPlayerStats ReadRecord(BinaryReader reader)
    {
        var matchPlayerStats = new EndMatchPlayerStats();
        matchPlayerStats.Read(reader);
        return matchPlayerStats;
    }
}