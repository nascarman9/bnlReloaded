using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectDamageBlocks : InstEffect
{
    public override InstEffectType Type => InstEffectType.DamageBlocks;

    public Damage? Damage { get; set; }

    public float Range { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, Damage != null, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      if (Damage != null)
        Damage.WriteRecord(writer, Damage);
      writer.Write(Range);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      Damage = bitField[3] ? Damage.ReadRecord(reader) : null;
      if (!bitField[4])
        return;
      Range = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectDamageBlocks value)
    {
      value.Write(writer);
    }

    public static InstEffectDamageBlocks ReadRecord(BinaryReader reader)
    {
      var effectDamageBlocks = new InstEffectDamageBlocks();
      effectDamageBlocks.Read(reader);
      return effectDamageBlocks;
    }
}