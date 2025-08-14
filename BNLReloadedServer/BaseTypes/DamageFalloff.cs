using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class DamageFalloff
{
    public float ReductionCoeff { get; set; }

    public float MaxDamageRange { get; set; }

    public float MinDamageRange { get; set; }

    public float MaxRange { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true).Write(writer);
      writer.Write(ReductionCoeff);
      writer.Write(MaxDamageRange);
      writer.Write(MinDamageRange);
      writer.Write(MaxRange);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        ReductionCoeff = reader.ReadSingle();
      if (bitField[1])
        MaxDamageRange = reader.ReadSingle();
      if (bitField[2])
        MinDamageRange = reader.ReadSingle();
      if (!bitField[3])
        return;
      MaxRange = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, DamageFalloff value) => value.Write(writer);

    public static DamageFalloff ReadRecord(BinaryReader reader)
    {
      var damageFalloff = new DamageFalloff();
      damageFalloff.Read(reader);
      return damageFalloff;
    }
}