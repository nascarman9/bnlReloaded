using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectSelf : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.Self;

    public List<InstEffect>? IntervalEffects { get; set; }

    public float Interval { get; set; }

    public List<Key>? ConstantEffects { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, IntervalEffects != null, true, ConstantEffects != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (IntervalEffects != null)
        writer.WriteList(IntervalEffects, InstEffect.WriteVariant);
      writer.Write(Interval);
      if (ConstantEffects != null)
        writer.WriteList(ConstantEffects, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      IntervalEffects = bitField[1] ? reader.ReadList<InstEffect, List<InstEffect>>(InstEffect.ReadVariant) : null;
      if (bitField[2])
        Interval = reader.ReadSingle();
      ConstantEffects = bitField[3] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectSelf value)
    {
      value.Write(writer);
    }

    public static ConstEffectSelf ReadRecord(BinaryReader reader)
    {
      var constEffectSelf = new ConstEffectSelf();
      constEffectSelf.Read(reader);
      return constEffectSelf;
    }
}