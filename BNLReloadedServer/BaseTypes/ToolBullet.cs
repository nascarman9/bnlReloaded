using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<ToolBullet>))]
public abstract class ToolBullet : IJsonFactory<ToolBullet>
{
    public virtual float GetSpeed(float holdInterval) => 0.0f;

    public virtual float? GetHoldInterval() => null;

    public abstract ToolBulletType Type { get; }

    public static ToolBullet CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<ToolBulletType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, ToolBullet value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static ToolBullet ReadVariant(BinaryReader reader)
    {
        var toolBullet = Create(reader.ReadByteEnum<ToolBulletType>());
        toolBullet.Read(reader);
        return toolBullet;
    }

    public static ToolBullet Create(ToolBulletType type)
    {
        return type switch
        {
            ToolBulletType.Hitscan => new ToolBulletHitscan(),
            ToolBulletType.Projectile => new ToolBulletProjectile(),
            ToolBulletType.UnitProjectile => new ToolBulletUnitProjectile(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}