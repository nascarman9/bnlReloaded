using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class Recoil
{
    public float Angle { get; set; }

    public float AngleVariance { get; set; }

    public float Clearance { get; set; }

    public float Magnitude { get; set; }

    public float MagnitudeVariance { get; set; }

    public float FirstModifier { get; set; }

    public float Recovery { get; set; }

    public float HorizontalTolerance { get; set; }

    public float VerticalTolerance { get; set; }

    public bool PersistentRecovery { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true, true, true, true).Write(writer);
      writer.Write(Angle);
      writer.Write(AngleVariance);
      writer.Write(Clearance);
      writer.Write(Magnitude);
      writer.Write(MagnitudeVariance);
      writer.Write(FirstModifier);
      writer.Write(Recovery);
      writer.Write(HorizontalTolerance);
      writer.Write(VerticalTolerance);
      writer.Write(PersistentRecovery);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(10);
      bitField.Read(reader);
      if (bitField[0])
        Angle = reader.ReadSingle();
      if (bitField[1])
        AngleVariance = reader.ReadSingle();
      if (bitField[2])
        Clearance = reader.ReadSingle();
      if (bitField[3])
        Magnitude = reader.ReadSingle();
      if (bitField[4])
        MagnitudeVariance = reader.ReadSingle();
      if (bitField[5])
        FirstModifier = reader.ReadSingle();
      if (bitField[6])
        Recovery = reader.ReadSingle();
      if (bitField[7])
        HorizontalTolerance = reader.ReadSingle();
      if (bitField[8])
        VerticalTolerance = reader.ReadSingle();
      if (!bitField[9])
        return;
      PersistentRecovery = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, Recoil value) => value.Write(writer);

    public static Recoil ReadRecord(BinaryReader reader)
    {
      var recoil = new Recoil();
      recoil.Read(reader);
      return recoil;
    }
}