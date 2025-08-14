using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchStatsLogic
{
    public Dictionary<PlayerMatchStatType, Dictionary<ScoreType, float>>? Stats { get; set; }

    public Dictionary<PlayerMatchStatType, float>? Total { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Stats != null, Total != null).Write(writer);
      if (Stats != null)
        writer.WriteMap(Stats, writer.WriteByteEnum, item => writer.WriteMap(item, writer.WriteByteEnum, writer.Write));
      if (Total != null)
        writer.WriteMap(Total, writer.WriteByteEnum,writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      Stats = bitField[0] ? reader.ReadMap<PlayerMatchStatType, Dictionary<ScoreType, float>, Dictionary<PlayerMatchStatType, Dictionary<ScoreType, float>>>(reader.ReadByteEnum<PlayerMatchStatType>, () => reader.ReadMap<ScoreType, float, Dictionary<ScoreType, float>>(reader.ReadByteEnum<ScoreType>, reader.ReadSingle)) : null;
      Total = bitField[1] ? reader.ReadMap<PlayerMatchStatType, float, Dictionary<PlayerMatchStatType, float>>(reader.ReadByteEnum<PlayerMatchStatType>, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchStatsLogic value)
    {
      value.Write(writer);
    }

    public static MatchStatsLogic ReadRecord(BinaryReader reader)
    {
      var matchStatsLogic = new MatchStatsLogic();
      matchStatsLogic.Read(reader);
      return matchStatsLogic;
    }
}