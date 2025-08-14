using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class RoomIdTeam : RoomId
{
    public override RoomIdType Type => RoomIdType.Team;

    public TeamType Team { get; set; }

    public int LobbyId { get; set; }

    public int InstanceId { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.WriteByteEnum(Team);
        writer.Write(LobbyId);
        writer.Write(InstanceId);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            Team = reader.ReadByteEnum<TeamType>();
        if (bitField[1])
            LobbyId = reader.ReadInt32();
        if (!bitField[2])
            return;
        InstanceId = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, RoomIdTeam value) => value.Write(writer);

    public static RoomIdTeam ReadRecord(BinaryReader reader)
    {
        var roomIdTeam = new RoomIdTeam();
        roomIdTeam.Read(reader);
        return roomIdTeam;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != typeof (RoomIdTeam))
            return false;
        var roomIdTeam = (RoomIdTeam) obj;
        return Team == roomIdTeam.Team && LobbyId == roomIdTeam.LobbyId && InstanceId == roomIdTeam.InstanceId;
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode() ^ Team.GetHashCode() ^ LobbyId.GetHashCode() ^ InstanceId.GetHashCode();
    }
}