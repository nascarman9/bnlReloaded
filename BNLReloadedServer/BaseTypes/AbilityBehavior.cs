using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<AbilityBehavior>))]
public abstract class AbilityBehavior : IJsonFactory<AbilityBehavior>
{
    public abstract AbilityBehaviorType Type { get; }

    public static AbilityBehavior CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<AbilityBehaviorType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, AbilityBehavior value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static AbilityBehavior ReadVariant(BinaryReader reader)
    {
        var abilityBehavior = Create(reader.ReadByteEnum<AbilityBehaviorType>());
        abilityBehavior.Read(reader);
        return abilityBehavior;
    }

    public static AbilityBehavior Create(AbilityBehaviorType type)
    {
        return type switch
        {
            AbilityBehaviorType.Cast => new AbilityBehaviorCast(),
            AbilityBehaviorType.Trigger => new AbilityBehaviorTrigger(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}