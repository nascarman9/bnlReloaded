using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LeaverRatingLogic
{
    public float InitValue { get; set; }

    public float MinValue { get; set; }

    public float MaxValue { get; set; }

    public float CompletionGain { get; set; }

    public float LeaveGain { get; set; }

    public float WarningThreshold { get; set; }

    public float WarningRemovalThreshold { get; set; }

    public float PunishmentThreshold { get; set; }

    public float PunishmentRemovalThreshold { get; set; }

    public float GoldModifier { get; set; }

    public float XpModifier { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true, true, true, true, true).Write(writer);
      writer.Write(InitValue);
      writer.Write(MinValue);
      writer.Write(MaxValue);
      writer.Write(CompletionGain);
      writer.Write(LeaveGain);
      writer.Write(WarningThreshold);
      writer.Write(WarningRemovalThreshold);
      writer.Write(PunishmentThreshold);
      writer.Write(PunishmentRemovalThreshold);
      writer.Write(GoldModifier);
      writer.Write(XpModifier);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(11);
      bitField.Read(reader);
      if (bitField[0])
        InitValue = reader.ReadSingle();
      if (bitField[1])
        MinValue = reader.ReadSingle();
      if (bitField[2])
        MaxValue = reader.ReadSingle();
      if (bitField[3])
        CompletionGain = reader.ReadSingle();
      if (bitField[4])
        LeaveGain = reader.ReadSingle();
      if (bitField[5])
        WarningThreshold = reader.ReadSingle();
      if (bitField[6])
        WarningRemovalThreshold = reader.ReadSingle();
      if (bitField[7])
        PunishmentThreshold = reader.ReadSingle();
      if (bitField[8])
        PunishmentRemovalThreshold = reader.ReadSingle();
      if (bitField[9])
        GoldModifier = reader.ReadSingle();
      if (!bitField[10])
        return;
      XpModifier = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, LeaverRatingLogic value)
    {
      value.Write(writer);
    }

    public static LeaverRatingLogic ReadRecord(BinaryReader reader)
    {
      var leaverRatingLogic = new LeaverRatingLogic();
      leaverRatingLogic.Read(reader);
      return leaverRatingLogic;
    }
}