using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ExplosionData
{
    public float InnerRadius { get; set; }

    public float OuterRadius { get; set; }

    public List<ExplosionVibrationData>? Vibrations { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, Vibrations != null).Write(writer);
      writer.Write(InnerRadius);
      writer.Write(OuterRadius);
      if (Vibrations != null)
        writer.WriteList(Vibrations, ExplosionVibrationData.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        InnerRadius = reader.ReadSingle();
      if (bitField[1])
        OuterRadius = reader.ReadSingle();
      Vibrations = bitField[2] ? reader.ReadList<ExplosionVibrationData, List<ExplosionVibrationData>>(ExplosionVibrationData.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ExplosionData value) => value.Write(writer);

    public static ExplosionData ReadRecord(BinaryReader reader)
    {
      var explosionData = new ExplosionData();
      explosionData.Read(reader);
      return explosionData;
    }
}