using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectKnockback : InstEffect
{
    public override InstEffectType Type => InstEffectType.Knockback;

    public float Force { get; set; }

    public float MidairForce { get; set; }

    public float EffectRange { get; set; }

    public bool LinearFalloff { get; set; }

    public bool AffectCaster { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true, true, true, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(Force);
      writer.Write(MidairForce);
      writer.Write(EffectRange);
      writer.Write(LinearFalloff);
      writer.Write(AffectCaster);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        Force = reader.ReadSingle();
      if (bitField[4])
        MidairForce = reader.ReadSingle();
      if (bitField[5])
        EffectRange = reader.ReadSingle();
      if (bitField[6])
        LinearFalloff = reader.ReadBoolean();
      if (!bitField[7])
        return;
      AffectCaster = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectKnockback value)
    {
      value.Write(writer);
    }

    public static InstEffectKnockback ReadRecord(BinaryReader reader)
    {
      var instEffectKnockback = new InstEffectKnockback();
      instEffectKnockback.Read(reader);
      return instEffectKnockback;
    }
}