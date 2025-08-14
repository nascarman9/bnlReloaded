using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataTeslaCoil : UnitData
{
    public override UnitType Type => UnitType.TeslaCoil;

    public int MaxCharges { get; set; } = 1;

    public int InitCharges { get; set; }

    public float AttackDelay { get; set; }

    public float PropagationRange { get; set; }

    public float AttackRange { get; set; }

    public EffectTargeting? AttackTargeting { get; set; }

    public InstEffect? PropagationEffect { get; set; }

    public InstEffect? AttackEffect { get; set; }

    public InstEffect? HitEffect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, AttackTargeting != null, PropagationEffect != null,
        AttackEffect != null, HitEffect != null).Write(writer);
      writer.Write(MaxCharges);
      writer.Write(InitCharges);
      writer.Write(AttackDelay);
      writer.Write(PropagationRange);
      writer.Write(AttackRange);
      if (AttackTargeting != null)
        EffectTargeting.WriteRecord(writer, AttackTargeting);
      if (PropagationEffect != null)
        InstEffect.WriteVariant(writer, PropagationEffect);
      if (AttackEffect != null)
        InstEffect.WriteVariant(writer, AttackEffect);
      if (HitEffect != null)
        InstEffect.WriteVariant(writer, HitEffect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(9);
      bitField.Read(reader);
      if (bitField[0])
        MaxCharges = reader.ReadInt32();
      if (bitField[1])
        InitCharges = reader.ReadInt32();
      if (bitField[2])
        AttackDelay = reader.ReadSingle();
      if (bitField[3])
        PropagationRange = reader.ReadSingle();
      if (bitField[4])
        AttackRange = reader.ReadSingle();
      AttackTargeting = bitField[5] ? EffectTargeting.ReadRecord(reader) : null;
      PropagationEffect = bitField[6] ? InstEffect.ReadVariant(reader) : null;
      AttackEffect = bitField[7] ? InstEffect.ReadVariant(reader) : null;
      HitEffect = bitField[8] ? InstEffect.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataTeslaCoil value)
    {
      value.Write(writer);
    }

    public static UnitDataTeslaCoil ReadRecord(BinaryReader reader)
    {
      var unitDataTeslaCoil = new UnitDataTeslaCoil();
      unitDataTeslaCoil.Read(reader);
      return unitDataTeslaCoil;
    }
}