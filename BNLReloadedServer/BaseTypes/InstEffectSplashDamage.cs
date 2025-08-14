using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectSplashDamage : InstEffect
{
    public override InstEffectType Type => InstEffectType.SplashDamage;

    public Damage? Damage { get; set; }

    public float Radius { get; set; }

    public List<InstEffect>? UnitInstEffects { get; set; }

    public List<Key>? UnitConstEffects { get; set; }

    /*public override void FromJsonData(JsonNode json)
    {
      Interrupt = json["interrupt"]?.Deserialize<EffectInterrupt>();
      Targeting = json["targeting"]?.Deserialize<EffectTargeting>();
      Impact = json["impact"]?.Deserialize<Key>();
      Damage = json["damage"]?.Deserialize<Damage>();
      if (json["radius"] != null)
        Radius = json["radius"]!.Deserialize<float>();
      UnitInstEffects = Json.Read.List<InstEffect>(new Func<JsonData, InstEffect>(InstEffect.CreateFromJson))(json.Object["unit_inst_effects"]);
      UnitConstEffects = Json.Read.List<Key>(new Func<JsonData, Key>(JsonHelper.KeyFromJson))(json.Object["unit_const_effects"]);
    }

    public override JsonNode ToJsonData() => JsonSerializer.SerializeToNode(this, GetType(), JsonHelper.DefaultSerializerSettings)!;*/

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, Damage != null, true, UnitInstEffects != null, UnitConstEffects != null).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      if (Damage != null)
        Damage.WriteRecord(writer, Damage);
      writer.Write(Radius);
      if (UnitInstEffects != null)
        writer.WriteList(UnitInstEffects, WriteVariant);
      if (UnitConstEffects != null)
        writer.WriteList(UnitConstEffects, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      Damage = bitField[3] ? Damage.ReadRecord(reader) : null;
      if (bitField[4])
        Radius = reader.ReadSingle();
      UnitInstEffects = bitField[5] ? reader.ReadList<InstEffect, List<InstEffect>>(ReadVariant) : null;
      UnitConstEffects = bitField[6] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectSplashDamage value)
    {
      value.Write(writer);
    }

    public static InstEffectSplashDamage ReadRecord(BinaryReader reader)
    {
      var effectSplashDamage = new InstEffectSplashDamage();
      effectSplashDamage.Read(reader);
      return effectSplashDamage;
    }
}