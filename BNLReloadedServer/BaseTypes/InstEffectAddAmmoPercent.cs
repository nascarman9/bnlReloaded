using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectAddAmmoPercent : InstEffect
{
    public override InstEffectType Type => InstEffectType.AddAmmoPercent;

    public float Fraction { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(Fraction);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (!bitField[3])
        return;
      Fraction = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectAddAmmoPercent value)
    {
      value.Write(writer);
    }

    public static InstEffectAddAmmoPercent ReadRecord(BinaryReader reader)
    {
      var effectAddAmmoPercent = new InstEffectAddAmmoPercent();
      effectAddAmmoPercent.Read(reader);
      return effectAddAmmoPercent;
    }
}