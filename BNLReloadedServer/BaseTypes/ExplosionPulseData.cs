using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ExplosionPulseData
{
    public float MaxStartValue { get; set; }

    public float MaxEndValue { get; set; }

    public float MinStartValue { get; set; }

    public float MinEndValue { get; set; }

    public int Duration { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true).Write(writer);
      writer.Write(MaxStartValue);
      writer.Write(MaxEndValue);
      writer.Write(MinStartValue);
      writer.Write(MinEndValue);
      writer.Write(Duration);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      if (bitField[0])
        MaxStartValue = reader.ReadSingle();
      if (bitField[1])
        MaxEndValue = reader.ReadSingle();
      if (bitField[2])
        MinStartValue = reader.ReadSingle();
      if (bitField[3])
        MinEndValue = reader.ReadSingle();
      if (!bitField[4])
        return;
      Duration = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, ExplosionPulseData value)
    {
      value.Write(writer);
    }

    public static ExplosionPulseData ReadRecord(BinaryReader reader)
    {
      var explosionPulseData = new ExplosionPulseData();
      explosionPulseData.Read(reader);
      return explosionPulseData;
    }
}