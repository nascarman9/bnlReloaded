using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectBunch : InstEffect
{
    public override InstEffectType Type => InstEffectType.Bunch;

    public bool BreakOnEffectFail { get; set; }

    public List<InstEffect>? Instant { get; set; }

    public List<Key>? Constant { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, Instant != null, Constant != null).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.Write(BreakOnEffectFail);
      if (Instant != null)
        writer.WriteList(Instant, WriteVariant);
      if (Constant != null)
        writer.WriteList(Constant, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        BreakOnEffectFail = reader.ReadBoolean();
      Instant = bitField[4] ? reader.ReadList<InstEffect, List<InstEffect>>(ReadVariant) : null;
      Constant = bitField[5] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectBunch value)
    {
      value.Write(writer);
    }

    public static InstEffectBunch ReadRecord(BinaryReader reader)
    {
      var instEffectBunch = new InstEffectBunch();
      instEffectBunch.Read(reader);
      return instEffectBunch;
    }
}