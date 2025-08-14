using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class Capturing
{
    public float CaptureRadius { get; set; }

    public float BaseCaptureTime { get; set; }

    public float MultiplierRate { get; set; }

    public float DecayRate { get; set; }

    public bool Recapturable { get; set; }

    public ResourceReward? CaptureReward { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, CaptureReward != null).Write(writer);
      writer.Write(CaptureRadius);
      writer.Write(BaseCaptureTime);
      writer.Write(MultiplierRate);
      writer.Write(DecayRate);
      writer.Write(Recapturable);
      if (CaptureReward == null)
        return;
      ResourceReward.WriteRecord(writer, CaptureReward);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      if (bitField[0])
        CaptureRadius = reader.ReadSingle();
      if (bitField[1])
        BaseCaptureTime = reader.ReadSingle();
      if (bitField[2])
        MultiplierRate = reader.ReadSingle();
      if (bitField[3])
        DecayRate = reader.ReadSingle();
      if (bitField[4])
        Recapturable = reader.ReadBoolean();
      CaptureReward = bitField[5] ? ResourceReward.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, Capturing value) => value.Write(writer);

    public static Capturing ReadRecord(BinaryReader reader)
    {
      var capturing = new Capturing();
      capturing.Read(reader);
      return capturing;
    }
}