using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<BlockSpecial>))]
public abstract class BlockSpecial : IJsonFactory<BlockSpecial>
{
    public abstract BlockSpecialType Type { get; }

    public static BlockSpecial CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<BlockSpecialType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, BlockSpecial value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static BlockSpecial ReadVariant(BinaryReader reader)
    {
        var blockSpecial = Create(reader.ReadByteEnum<BlockSpecialType>());
        blockSpecial.Read(reader);
        return blockSpecial;
    }

    public static BlockSpecial Create(BlockSpecialType type)
    {
        return type switch
        {
            BlockSpecialType.InsideEffect => new BlockSpecialInsideEffect(),
            BlockSpecialType.Bounce => new BlockSpecialBounce(),
            BlockSpecialType.Slow => new BlockSpecialSlow(),
            BlockSpecialType.Slippery => new BlockSpecialSlippery(),
            BlockSpecialType.FastMovement => new BlockSpecialFastMovement(),
            BlockSpecialType.External => new BlockSpecialExternal(),
            BlockSpecialType.NoFallDamage => new BlockSpecialNoFallDamage(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}