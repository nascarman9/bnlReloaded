using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<AbilityApplication>))]
public abstract class AbilityApplication : IJsonFactory<AbilityApplication>
{
    public abstract AbilityApplicationType Type { get; }

    public static AbilityApplication CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<AbilityApplicationType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, AbilityApplication value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static AbilityApplication ReadVariant(BinaryReader reader)
    {
        var abilityApplication = Create(reader.ReadByteEnum<AbilityApplicationType>());
        abilityApplication.Read(reader);
        return abilityApplication;
    }

    public static AbilityApplication Create(AbilityApplicationType type)
    {
        return type switch
        {
            AbilityApplicationType.Self => new AbilityApplicationSelf(),
            AbilityApplicationType.Hitscan => new AbilityApplicationHitscan(),
            AbilityApplicationType.Projectile => new AbilityApplicationProjectile(),
            AbilityApplicationType.UnitProjectile => new AbilityApplicationUnitProjectile(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}