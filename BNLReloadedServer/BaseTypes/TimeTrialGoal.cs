using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class TimeTrialGoal
{
    public float? CompletionSeconds { get; set; }

    public Dictionary<CurrencyType, float>? RewardCurrency { get; set; }

    public float RewardXp { get; set; }

    public LocalizedString? Description { get; set; }

    public LocalizedString? ShortDescription { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(CompletionSeconds.HasValue, RewardCurrency != null, true, Description != null, ShortDescription != null).Write(writer);
      if (CompletionSeconds.HasValue)
        writer.Write(CompletionSeconds.Value);
      if (RewardCurrency != null)
        writer.WriteMap(RewardCurrency, writer.WriteByteEnum, writer.Write);
      writer.Write(RewardXp);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (ShortDescription != null)
        LocalizedString.WriteRecord(writer, ShortDescription);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      CompletionSeconds = bitField[0] ? reader.ReadSingle() : null;
      RewardCurrency = bitField[1] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
      if (bitField[2])
        RewardXp = reader.ReadSingle();
      Description = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      ShortDescription = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, TimeTrialGoal value) => value.Write(writer);

    public static TimeTrialGoal ReadRecord(BinaryReader reader)
    {
      var timeTrialGoal = new TimeTrialGoal();
      timeTrialGoal.Read(reader);
      return timeTrialGoal;
    }
}