using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ExplosionVibrationData
{
    public int Time { get; set; }

    public Dictionary<PulseType, ExplosionPulseData>? Pulses { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, Pulses != null).Write(writer);
      writer.Write(Time);
      if (Pulses != null)
        writer.WriteMap(Pulses, writer.WriteByteEnum, ExplosionPulseData.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      if (bitField[0])
        Time = reader.ReadInt32();
      Pulses = bitField[1] ? reader.ReadMap<PulseType, ExplosionPulseData, Dictionary<PulseType, ExplosionPulseData>>(reader.ReadByteEnum<PulseType>, ExplosionPulseData.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ExplosionVibrationData value)
    {
      value.Write(writer);
    }

    public static ExplosionVibrationData ReadRecord(BinaryReader reader)
    {
      var explosionVibrationData = new ExplosionVibrationData();
      explosionVibrationData.Read(reader);
      return explosionVibrationData;
    }
}