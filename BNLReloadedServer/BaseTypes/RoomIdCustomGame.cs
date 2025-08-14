using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class RoomIdCustomGame : RoomId
{
    public override RoomIdType Type => RoomIdType.CustomGame;

    public ulong CustomGameId { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(CustomGameId);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        CustomGameId = reader.ReadUInt64();
    }

    public static void WriteRecord(BinaryWriter writer, RoomIdCustomGame value)
    {
        value.Write(writer);
    }

    public static RoomIdCustomGame ReadRecord(BinaryReader reader)
    {
        var roomIdCustomGame = new RoomIdCustomGame();
        roomIdCustomGame.Read(reader);
        return roomIdCustomGame;
    }

    public override bool Equals(object? obj)
    {
        return obj != null && obj.GetType() == typeof (RoomIdCustomGame) && (long) CustomGameId == (long) ((RoomIdCustomGame) obj).CustomGameId;
    }

    public override int GetHashCode() => Type.GetHashCode() ^ CustomGameId.GetHashCode();
}