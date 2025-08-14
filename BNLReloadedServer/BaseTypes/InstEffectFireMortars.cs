using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectFireMortars : InstEffect
{
    public override InstEffectType Type => InstEffectType.FireMortars;

    public int? MaxMortars { get; set; }

    public bool OwnedMortarsOnly { get; set; }

    public float BaseSpread { get; set; }

    public float IncrementalSpread { get; set; }

    public float DistanceSpread { get; set; }

    public float MinSpreadPercent { get; set; }

    public float BaseFireDelay { get; set; }

    public float FireDelayModifier { get; set; }

    public InstEffect? MortarFireEffect { get; set; }

    public InstEffect? HitEffect { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, MaxMortars.HasValue, true, true, true, true, true, true, true, MortarFireEffect != null, HitEffect != null).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      if (MaxMortars.HasValue)
        writer.Write(MaxMortars.Value);
      writer.Write(OwnedMortarsOnly);
      writer.Write(BaseSpread);
      writer.Write(IncrementalSpread);
      writer.Write(DistanceSpread);
      writer.Write(MinSpreadPercent);
      writer.Write(BaseFireDelay);
      writer.Write(FireDelayModifier);
      if (MortarFireEffect != null)
        WriteVariant(writer, MortarFireEffect);
      if (HitEffect == null)
        return;
      WriteVariant(writer, HitEffect);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(13);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      MaxMortars = bitField[3] ? reader.ReadInt32() : null;
      if (bitField[4])
        OwnedMortarsOnly = reader.ReadBoolean();
      if (bitField[5])
        BaseSpread = reader.ReadSingle();
      if (bitField[6])
        IncrementalSpread = reader.ReadSingle();
      if (bitField[7])
        DistanceSpread = reader.ReadSingle();
      if (bitField[8])
        MinSpreadPercent = reader.ReadSingle();
      if (bitField[9])
        BaseFireDelay = reader.ReadSingle();
      if (bitField[10])
        FireDelayModifier = reader.ReadSingle();
      MortarFireEffect = bitField[11] ? ReadVariant(reader) : null;
      HitEffect = bitField[12] ? ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectFireMortars value)
    {
      value.Write(writer);
    }

    public static InstEffectFireMortars ReadRecord(BinaryReader reader)
    {
      var effectFireMortars = new InstEffectFireMortars();
      effectFireMortars.Read(reader);
      return effectFireMortars;
    }
}