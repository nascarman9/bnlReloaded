using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<PerkMod>))]
public abstract class PerkMod : IJsonFactory<PerkMod>
{
    public abstract PerkModType Type { get; }

    public static PerkMod CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<PerkModType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, PerkMod value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static PerkMod ReadVariant(BinaryReader reader)
    {
        var perkMod = Create(reader.ReadByteEnum<PerkModType>());
        perkMod.Read(reader);
        return perkMod;
    }

    public static PerkMod Create(PerkModType type)
    {
        return type switch
        {
            PerkModType.Effect => new PerkModEffect(),
            PerkModType.Device => new PerkModDevice(),
            PerkModType.Ability => new PerkModAbility(),
            PerkModType.Passive => new PerkModPassive(),
            PerkModType.Gear => new PerkModGear(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}