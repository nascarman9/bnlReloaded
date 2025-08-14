using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<UnitMovement>))]
public abstract class UnitMovement : IJsonFactory<UnitMovement>
{
    public abstract UnitMovementType Type { get; }

    public static UnitMovement CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<UnitMovementType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, UnitMovement value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static UnitMovement ReadVariant(BinaryReader reader)
    {
        var unitMovement = Create(reader.ReadByteEnum<UnitMovementType>());
        unitMovement.Read(reader);
        return unitMovement;
    }

    public static UnitMovement Create(UnitMovementType type)
    {
        return type switch
        {
            UnitMovementType.Static => new UnitMovementStatic(),
            UnitMovementType.Custom => new UnitMovementCustom(),
            UnitMovementType.Falling => new UnitMovementFalling(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}