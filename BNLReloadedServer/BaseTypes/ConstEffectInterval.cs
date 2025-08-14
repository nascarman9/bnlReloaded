using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectInterval : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.Interval;

    public float Interval { get; set; }

    public List<InstEffect>? IntervalEffects { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, true, IntervalEffects != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      writer.Write(Interval);
      if (IntervalEffects != null)
        writer.WriteList(IntervalEffects, InstEffect.WriteVariant);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      if (bitField[1])
        Interval = reader.ReadSingle();
      IntervalEffects = bitField[2] ? reader.ReadList<InstEffect, List<InstEffect>>(InstEffect.ReadVariant) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectInterval value)
    {
      value.Write(writer);
    }

    public static ConstEffectInterval ReadRecord(BinaryReader reader)
    {
      var constEffectInterval = new ConstEffectInterval();
      constEffectInterval.Read(reader);
      return constEffectInterval;
    }
}