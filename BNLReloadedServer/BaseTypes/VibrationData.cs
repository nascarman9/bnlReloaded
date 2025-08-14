using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class VibrationData
{
    public int Time { get; set; }

    public Dictionary<PulseType, PulseData> Pulses { get; set; } = new();

    public List<string>? BreakEvents { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, BreakEvents != null).Write(writer);
      writer.Write(Time);
      writer.WriteMap(Pulses, writer.WriteByteEnum, PulseData.WriteRecord);
      if (BreakEvents != null)
        writer.WriteList(BreakEvents, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        Time = reader.ReadInt32();
      if (bitField[1])
        Pulses = reader.ReadMap<PulseType, PulseData, Dictionary<PulseType, PulseData>>(reader.ReadByteEnum<PulseType>, PulseData.ReadRecord);
      BreakEvents = bitField[2] ? reader.ReadList<string, List<string>>(reader.ReadString) : null;
    }

    public static void WriteRecord(BinaryWriter writer, VibrationData value) => value.Write(writer);

    public static VibrationData ReadRecord(BinaryReader reader)
    {
      var vibrationData = new VibrationData();
      vibrationData.Read(reader);
      return vibrationData;
    }
}