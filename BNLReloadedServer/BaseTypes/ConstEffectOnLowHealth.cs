using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectOnLowHealth : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.OnLowHealth;

    public float HealthThreshold { get; set; }

    public List<Key>? ConstantEffects { get; set; }

    public InstEffect? OnThresholdDown { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, true, ConstantEffects != null, OnThresholdDown != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      writer.Write(HealthThreshold);
      if (ConstantEffects != null)
        writer.WriteList(ConstantEffects, Key.WriteRecord);
      if (OnThresholdDown == null)
        return;
      InstEffect.WriteVariant(writer, OnThresholdDown);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      if (bitField[1])
        HealthThreshold = reader.ReadSingle();
      ConstantEffects = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      OnThresholdDown = bitField[3] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectOnLowHealth value)
    {
      value.Write(writer);
    }

    public static ConstEffectOnLowHealth ReadRecord(BinaryReader reader)
    {
      var effectOnLowHealth = new ConstEffectOnLowHealth();
      effectOnLowHealth.Read(reader);
      return effectOnLowHealth;
    }
}