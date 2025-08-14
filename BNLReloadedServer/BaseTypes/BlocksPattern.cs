using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<BlocksPattern>))]
public abstract class BlocksPattern : IJsonFactory<BlocksPattern>
{
    public abstract BlocksPatternType Type { get; }

    public static BlocksPattern CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<BlocksPatternType>());
    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, BlocksPattern value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static BlocksPattern ReadVariant(BinaryReader reader)
    {
        var blocksPattern = Create(reader.ReadByteEnum<BlocksPatternType>());
        blocksPattern.Read(reader);
        return blocksPattern;
    }

    public static BlocksPattern Create(BlocksPatternType type)
    {
        return type switch
        {
            BlocksPatternType.One => new BlocksPatternOne(),
            BlocksPatternType.Sphere => new BlocksPatternSphere(),
            BlocksPatternType.Spit => new BlocksPatternSpit(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}