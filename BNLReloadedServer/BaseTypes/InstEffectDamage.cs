using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectDamage : InstEffect
{
  public override InstEffectType Type => InstEffectType.Damage;

    public Damage? Damage { get; set; }

    public float CritModifier { get; set; } = 1f;

    public DamageFalloff? Falloff { get; set; }

    /*public override void FromJsonData(JsonNode json)
    {
      Interrupt = json["interrupt"]?.Deserialize<EffectInterrupt>();
      Targeting = json["targeting"]?.Deserialize<EffectTargeting>();
      Impact = json["impact"]?.Deserialize<Key>();
      Damage = json["damage"]?.Deserialize<Damage>();
      if (json["crit_modifier"] != null)
        CritModifier = json["crit_modifier"]!.Deserialize<float>()!;
      Falloff = json["falloff"]?.Deserialize<DamageFalloff>();
    }

    public override JsonNode ToJsonData() => JsonSerializer.SerializeToNode(this, GetType(), JsonHelper.DefaultSerializerSettings)!;*/

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true, Falloff != null).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      if (Damage != null)
        Damage.WriteRecord(writer, Damage);
      writer.Write(CritModifier);
      if (Falloff == null)
        return;
      DamageFalloff.WriteRecord(writer, Falloff);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      Damage = bitField[3] ? Damage.ReadRecord(reader) : null;
      if (bitField[4])
        CritModifier = reader.ReadSingle();
      Falloff = bitField[5] ? DamageFalloff.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectDamage value)
    {
      value.Write(writer);
    }

    public static InstEffectDamage ReadRecord(BinaryReader reader)
    {
      var instEffectDamage = new InstEffectDamage();
      instEffectDamage.Read(reader);
      return instEffectDamage;
    }
}