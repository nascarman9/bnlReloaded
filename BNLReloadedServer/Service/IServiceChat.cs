using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServiceChat : IService
{
    public void SendIgnores(List<ChatPlayer> players);
    public void SendRoomAdd(RoomId room);
    public void SendRoomRemove(RoomId room);
    public void SendPrivateMessage(ChatPlayer from, ChatPlayer to, string message);
    public void SendPrivateMessageFailed(uint toId, PrivateMessageFailReason reason);
    public void SendRoomMessage(RoomId roomId, ChatPlayer player, string message);
    public void SendServiceMessage(RoomId? roomId, string message, bool isLocalized, Dictionary<string, string> arguments);
}