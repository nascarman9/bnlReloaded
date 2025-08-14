using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<InstEffect>))]
public abstract class InstEffect : IJsonFactory<InstEffect>
{
    public abstract InstEffectType Type { get; }

    public EffectInterrupt? Interrupt { get; set; }

    public EffectTargeting? Targeting { get; set; }

    public Key? Impact { get; set; }

    public static InstEffect CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<InstEffectType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, InstEffect value)
    {
      writer.WriteByteEnum(value.Type);
      value.Write(writer);
    }

    public static InstEffect ReadVariant(BinaryReader reader)
    {
      var instEffect = Create(reader.ReadByteEnum<InstEffectType>());
      instEffect.Read(reader);
      return instEffect;
    }

    public static InstEffect Create(InstEffectType type)
    {
      return type switch
      {
        InstEffectType.Damage => new InstEffectDamage(),
        InstEffectType.SplashDamage => new InstEffectSplashDamage(),
        InstEffectType.Heal => new InstEffectHeal(),
        InstEffectType.Supply => new InstEffectSupply(),
        InstEffectType.AddAmmo => new InstEffectAddAmmo(),
        InstEffectType.AddAmmoPercent => new InstEffectAddAmmoPercent(),
        InstEffectType.DrainAmmo => new InstEffectDrainAmmo(),
        InstEffectType.DrainMagazineAmmo => new InstEffectDrainMagazineAmmo(),
        InstEffectType.AddResource => new InstEffectAddResource(),
        InstEffectType.UnitSpawn => new InstEffectUnitSpawn(),
        InstEffectType.BlocksSpawn => new InstEffectBlocksSpawn(),
        InstEffectType.BuildDevice => new InstEffectBuildDevice(),
        InstEffectType.Bunch => new InstEffectBunch(),
        InstEffectType.AllUnitsBunch => new InstEffectAllUnitsBunch(),
        InstEffectType.CasterBunch => new InstEffectCasterBunch(),
        InstEffectType.HealBlocks => new InstEffectHealBlocks(),
        InstEffectType.DamageBlocks => new InstEffectDamageBlocks(),
        InstEffectType.Teleport => new InstEffectTeleport(),
        InstEffectType.TeleportTo => new InstEffectTeleportTo(),
        InstEffectType.FireMortars => new InstEffectFireMortars(),
        InstEffectType.Knockback => new InstEffectKnockback(),
        InstEffectType.Purge => new InstEffectPurge(),
        InstEffectType.InstReload => new InstEffectInstReload(),
        InstEffectType.Kill => new InstEffectKill(),
        InstEffectType.ChargeTesla => new InstEffectChargeTesla(),
        InstEffectType.AllPlayersPersistent => new InstEffectAllPlayersPersistent(),
        InstEffectType.ResourceAll => new InstEffectResourceAll(),
        InstEffectType.ZoneEffect => new InstEffectZoneEffect(),
        InstEffectType.Slip => new InstEffectSlip(),
        InstEffectType.ReplaceBlocks => new InstEffectReplaceBlocks(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
      };
    }
}