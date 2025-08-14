using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class RoomIdGlobal : RoomId
{
    public override RoomIdType Type => RoomIdType.Global;

    public override void Write(BinaryWriter writer) => new BitField().Write(writer);

    public override void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, RoomIdGlobal value) => value.Write(writer);

    public static RoomIdGlobal ReadRecord(BinaryReader reader)
    {
        var roomIdGlobal = new RoomIdGlobal();
        roomIdGlobal.Read(reader);
        return roomIdGlobal;
    }

    public override bool Equals(object? obj)
    {
        return obj != null && obj.GetType() == typeof (RoomIdGlobal);
    }

    public override int GetHashCode() => Type.GetHashCode();
}