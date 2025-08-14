using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BackfillingLogic
{
    public float PlayerDeviationIncrease { get; set; }

    public float ObjectivesHealthThreshold { get; set; } = 0.1f;

    public float ObjectiveDamageTimeout { get; set; } = 60f;

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true).Write(writer);
      writer.Write(PlayerDeviationIncrease);
      writer.Write(ObjectivesHealthThreshold);
      writer.Write(ObjectiveDamageTimeout);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        PlayerDeviationIncrease = reader.ReadSingle();
      if (bitField[1])
        ObjectivesHealthThreshold = reader.ReadSingle();
      if (!bitField[2])
        return;
      ObjectiveDamageTimeout = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, BackfillingLogic value)
    {
      value.Write(writer);
    }

    public static BackfillingLogic ReadRecord(BinaryReader reader)
    {
      var backfillingLogic = new BackfillingLogic();
      backfillingLogic.Read(reader);
      return backfillingLogic;
    }
}