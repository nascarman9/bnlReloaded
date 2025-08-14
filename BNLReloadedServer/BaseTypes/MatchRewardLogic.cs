using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchRewardLogic
{
    public Dictionary<MatchRewardBonusType, float>? Bonuses { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Bonuses != null).Write(writer);
      if (Bonuses != null)
        writer.WriteMap(Bonuses, writer.WriteByteEnum, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(1);
      bitField.Read(reader);
      Bonuses = bitField[0] ? reader.ReadMap<MatchRewardBonusType, float, Dictionary<MatchRewardBonusType, float>>(reader.ReadByteEnum<MatchRewardBonusType>, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchRewardLogic value)
    {
      value.Write(writer);
    }

    public static MatchRewardLogic ReadRecord(BinaryReader reader)
    {
      var matchRewardLogic = new MatchRewardLogic();
      matchRewardLogic.Read(reader);
      return matchRewardLogic;
    }
}