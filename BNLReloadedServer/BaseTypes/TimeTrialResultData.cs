using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class TimeTrialResultData
{
    public List<int>? OldGoalsCompleted { get; set; }

    public List<int>? NewGoalsCompleted { get; set; }

    public float XpReward { get; set; }

    public Dictionary<CurrencyType, float>? CurrencyReward { get; set; }

    public float ResultTime { get; set; }

    public float? BestResultTime { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(OldGoalsCompleted != null, NewGoalsCompleted != null, true, CurrencyReward != null, true, BestResultTime.HasValue).Write(writer);
      if (OldGoalsCompleted != null)
        writer.WriteList(OldGoalsCompleted, writer.Write);;
      if (NewGoalsCompleted != null)
        writer.WriteList(NewGoalsCompleted, writer.Write);
      writer.Write(XpReward);
      if (CurrencyReward != null)
        writer.WriteMap(CurrencyReward, writer.WriteByteEnum, writer.Write);
      writer.Write(ResultTime);
      if (!BestResultTime.HasValue)
        return;
      writer.Write(BestResultTime.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      OldGoalsCompleted = bitField[0] ? reader.ReadList<int, List<int>>(reader.ReadInt32) : null;
      NewGoalsCompleted = bitField[1] ? reader.ReadList<int, List<int>>(reader.ReadInt32) : null;
      if (bitField[2])
        XpReward = reader.ReadSingle();
      CurrencyReward = bitField[3] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
      if (bitField[4])
        ResultTime = reader.ReadSingle();
      BestResultTime = bitField[5] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, TimeTrialResultData value)
    {
      value.Write(writer);
    }

    public static TimeTrialResultData ReadRecord(BinaryReader reader)
    {
      var timeTrialResultData = new TimeTrialResultData();
      timeTrialResultData.Read(reader);
      return timeTrialResultData;
    }
}