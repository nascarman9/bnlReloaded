using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ServerTypes;

public record InstanceChatRooms(ChatRoom Team1Room, ChatRoom Team2Room, ChatRoom BothTeamsRoom)
{
    public ChatRoom? this[RoomId roomId]
    {
        get
        {
            if (Team1Room.RoomId.Equals(roomId))
                return Team1Room;
            if (Team2Room.RoomId.Equals(roomId))
                return Team2Room;
            return BothTeamsRoom.RoomId.Equals(roomId) ? BothTeamsRoom : null;
        }
    }

    public void ClearRooms()
    {
        Team1Room.ClearRoom();
        Team2Room.ClearRoom();
        BothTeamsRoom.ClearRoom();
    }
}