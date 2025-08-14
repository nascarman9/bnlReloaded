using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public abstract class MapInfo
{
    public abstract MapInfoType Type { get; }

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, MapInfo value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static MapInfo ReadVariant(BinaryReader reader)
    {
        var mapInfo = Create(reader.ReadByteEnum<MapInfoType>());
        mapInfo.Read(reader);
        return mapInfo;
    }

    public static MapInfo Create(MapInfoType type)
    {
        return type switch
        {
            MapInfoType.Card => new MapInfoCard(),
            MapInfoType.Steam => new MapInfoSteam(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}