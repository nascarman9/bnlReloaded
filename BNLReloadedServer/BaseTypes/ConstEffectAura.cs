using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectAura : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.Aura;

    public float OuterRadius { get; set; }

    public float? InnerRadius { get; set; }

    public List<InstEffect>? IntervalEffects { get; set; }

    public float Interval { get; set; }

    public InstEffect? EnterEffect { get; set; }

    public InstEffect? LeaveEffect { get; set; }

    public List<Key>? ConstantEffects { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, true, InnerRadius.HasValue, IntervalEffects != null, true, EnterEffect != null,
        LeaveEffect != null, ConstantEffects != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      writer.Write(OuterRadius);
      if (InnerRadius.HasValue)
        writer.Write(InnerRadius.Value);
      if (IntervalEffects != null)
        writer.WriteList(IntervalEffects, InstEffect.WriteVariant);
      writer.Write(Interval);
      if (EnterEffect != null)
        InstEffect.WriteVariant(writer, EnterEffect);
      if (LeaveEffect != null)
        InstEffect.WriteVariant(writer, LeaveEffect);
      if (ConstantEffects != null)
        writer.WriteList(ConstantEffects, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      if (bitField[1])
        OuterRadius = reader.ReadSingle();
      InnerRadius = bitField[2] ? reader.ReadSingle() : null;
      IntervalEffects = bitField[3] ? reader.ReadList<InstEffect, List<InstEffect>>(InstEffect.ReadVariant) : null;
      if (bitField[4])
        Interval = reader.ReadSingle();
      EnterEffect = bitField[5] ? InstEffect.ReadVariant(reader) : null;
      LeaveEffect = bitField[6] ? InstEffect.ReadVariant(reader) : null;
      ConstantEffects = bitField[7] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectAura value)
    {
      value.Write(writer);
    }

    public static ConstEffectAura ReadRecord(BinaryReader reader)
    {
      var constEffectAura = new ConstEffectAura();
      constEffectAura.Read(reader);
      return constEffectAura;
    }
}