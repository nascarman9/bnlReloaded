using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<ConstEffect>))]
public abstract class ConstEffect : IJsonFactory<ConstEffect>
{
    public abstract ConstEffectType Type { get; }

    public EffectTargeting? Targeting { get; set; }

    public static ConstEffect CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<ConstEffectType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, ConstEffect value)
    {
      writer.WriteByteEnum(value.Type);
      value.Write(writer);
    }

    public static ConstEffect ReadVariant(BinaryReader reader)
    {
      var constEffect = Create(reader.ReadByteEnum<ConstEffectType>());
      constEffect.Read(reader);
      return constEffect;
    }

    public static ConstEffect Create(ConstEffectType type)
    {
      return type switch
      {
        ConstEffectType.Buff => new ConstEffectBuff(),
        ConstEffectType.Immunity => new ConstEffectImmunity(),
        ConstEffectType.Aura => new ConstEffectAura(),
        ConstEffectType.Self => new ConstEffectSelf(),
        ConstEffectType.Team => new ConstEffectTeam(),
        ConstEffectType.OnSprint => new ConstEffectOnSprint(),
        ConstEffectType.OnDeath => new ConstEffectOnDeath(),
        ConstEffectType.OnFall => new ConstEffectOnFall(),
        ConstEffectType.OnKill => new ConstEffectOnKill(),
        ConstEffectType.OnBuilding => new ConstEffectOnBuilding(),
        ConstEffectType.OnLowHealth => new ConstEffectOnLowHealth(),
        ConstEffectType.OnMatchContext => new ConstEffectOnMatchContext(),
        ConstEffectType.OnDamageTaken => new ConstEffectOnDamageTaken(),
        ConstEffectType.OnGearSwitch => new ConstEffectOnGearSwitch(),
        ConstEffectType.Pull => new ConstEffectPull(),
        ConstEffectType.OnReload => new ConstEffectOnReload(),
        ConstEffectType.Interval => new ConstEffectInterval(),
        ConstEffectType.OnNearbyBlock => new ConstEffectOnNearbyBlock(),
        ConstEffectType.OnLeading => throw new ArgumentOutOfRangeException(nameof(type), type, "OnLeading ConstEffect not supported"),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
      };
    }
}