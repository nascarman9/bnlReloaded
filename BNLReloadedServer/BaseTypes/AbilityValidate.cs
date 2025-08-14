using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<AbilityValidate>))]
public abstract class AbilityValidate : IJsonFactory<AbilityValidate>
{
    public abstract AbilityValidateType Type { get; }

    public static AbilityValidate CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<AbilityValidateType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, AbilityValidate value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static AbilityValidate ReadVariant(BinaryReader reader)
    {
        var abilityValidate = Create(reader.ReadByteEnum<AbilityValidateType>());
        abilityValidate.Read(reader);
        return abilityValidate;
    }

    public static AbilityValidate Create(AbilityValidateType type)
    {
        if (type == AbilityValidateType.Devices)
            return new AbilityValidateDevices();
        throw new ArgumentOutOfRangeException(nameof (type), type, "Invalid variant tag");
    }
}