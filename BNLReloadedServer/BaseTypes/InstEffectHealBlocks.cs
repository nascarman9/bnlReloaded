using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectHealBlocks : InstEffect
{
    public override InstEffectType Type => InstEffectType.HealBlocks;

    public float HealAmount { get; set; }

    public float Range { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(HealAmount);
      writer.Write(Range);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        HealAmount = reader.ReadSingle();
      if (!bitField[4])
        return;
      Range = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectHealBlocks value)
    {
      value.Write(writer);
    }

    public static InstEffectHealBlocks ReadRecord(BinaryReader reader)
    {
      var effectHealBlocks = new InstEffectHealBlocks();
      effectHealBlocks.Read(reader);
      return effectHealBlocks;
    }
}