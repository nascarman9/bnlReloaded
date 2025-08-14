using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectAllUnitsBunch : InstEffect
{
  public override InstEffectType Type => InstEffectType.AllUnitsBunch;

    public float? Range { get; set; }

    public bool BreakOnEffectFail { get; set; }

    public List<InstEffect>? Instant { get; set; }

    public List<Key>? Constant { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, Range.HasValue, true, Instant != null, Constant != null).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      if (Range.HasValue)
        writer.Write(Range.Value);
      writer.Write(BreakOnEffectFail);
      if (Instant != null)
        writer.WriteList(Instant, WriteVariant);
      if (Constant != null)
        writer.WriteList(Constant, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      Range = bitField[3] ? reader.ReadSingle() : null;
      if (bitField[4])
        BreakOnEffectFail = reader.ReadBoolean();
      Instant = bitField[5] ? reader.ReadList<InstEffect, List<InstEffect>>(ReadVariant) : null;
      Constant = bitField[6] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectAllUnitsBunch value)
    {
      value.Write(writer);
    }

    public static InstEffectAllUnitsBunch ReadRecord(BinaryReader reader)
    {
      var effectAllUnitsBunch = new InstEffectAllUnitsBunch();
      effectAllUnitsBunch.Read(reader);
      return effectAllUnitsBunch;
    }
}