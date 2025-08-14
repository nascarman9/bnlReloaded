using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class RoomIdSquad : RoomId
{
    public override RoomIdType Type => RoomIdType.Squad;

    public ulong SquadId { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(SquadId);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        SquadId = reader.ReadUInt64();
    }

    public static void WriteRecord(BinaryWriter writer, RoomIdSquad value) => value.Write(writer);

    public static RoomIdSquad ReadRecord(BinaryReader reader)
    {
        var roomIdSquad = new RoomIdSquad();
        roomIdSquad.Read(reader);
        return roomIdSquad;
    }

    public override bool Equals(object? obj)
    {
        return obj != null && obj.GetType() == typeof (RoomIdSquad) && (long) SquadId == (long) ((RoomIdSquad) obj).SquadId;
    }

    public override int GetHashCode() => Type.GetHashCode() ^ SquadId.GetHashCode();
}