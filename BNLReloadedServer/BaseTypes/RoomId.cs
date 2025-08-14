using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public abstract class RoomId
{
    public abstract RoomIdType Type { get; }

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, RoomId value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static RoomId ReadVariant(BinaryReader reader)
    {
        var roomId = Create(reader.ReadByteEnum<RoomIdType>());
        roomId.Read(reader);
        return roomId;
    }

    public static RoomId Create(RoomIdType type)
    {
        return type switch
        {
            RoomIdType.Team => new RoomIdTeam(),
            RoomIdType.Squad => new RoomIdSquad(),
            RoomIdType.CustomGame => new RoomIdCustomGame(),
            RoomIdType.Global => new RoomIdGlobal(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}