using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardRewardsLogic : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.RewardsLogic;

    public Key PlayTutorialVideo { get; set; }

    public Key CompleteFirstMatch { get; set; }

    public Key CompleteSecondMatch { get; set; }

    public float? DailyWinLpBonus { get; set; }

    public List<DailyLoginReward>? DailyLoginBase { get; set; }

    public List<DailyLoginReward>? DailyLoginLoop { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, true, true, true, DailyWinLpBonus.HasValue, DailyLoginBase != null, DailyLoginLoop != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      Key.WriteRecord(writer, PlayTutorialVideo);
      Key.WriteRecord(writer, CompleteFirstMatch);
      Key.WriteRecord(writer, CompleteSecondMatch);
      if (DailyWinLpBonus.HasValue)
        writer.Write(DailyWinLpBonus.Value);
      if (DailyLoginBase != null)
        writer.WriteList(DailyLoginBase, DailyLoginReward.WriteRecord);
      if (DailyLoginLoop != null)
        writer.WriteList(DailyLoginLoop, DailyLoginReward.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      if (bitField[2])
        PlayTutorialVideo = Key.ReadRecord(reader);
      if (bitField[3])
        CompleteFirstMatch = Key.ReadRecord(reader);
      if (bitField[4])
        CompleteSecondMatch = Key.ReadRecord(reader);
      DailyWinLpBonus = bitField[5] ? reader.ReadSingle() : null;
      DailyLoginBase = bitField[6] ? reader.ReadList<DailyLoginReward, List<DailyLoginReward>>(DailyLoginReward.ReadRecord) : null;
      DailyLoginLoop = bitField[7] ? reader.ReadList<DailyLoginReward, List<DailyLoginReward>>(DailyLoginReward.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardRewardsLogic value)
    {
      value.Write(writer);
    }

    public static CardRewardsLogic ReadRecord(BinaryReader reader)
    {
      var cardRewardsLogic = new CardRewardsLogic();
      cardRewardsLogic.Read(reader);
      return cardRewardsLogic;
    }
}