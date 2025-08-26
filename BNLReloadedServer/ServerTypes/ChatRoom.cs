using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Servers;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.ServerTypes;

public class ChatRoom(RoomId roomId, ISender sender)
{
    private readonly ServiceChat _chatService = new(sender);
    public readonly RoomId RoomId = roomId;

    public void AddToRoom(Guid sessionId, IServiceChat notifyPlayer)
    {
        sender.Subscribe(sessionId);
        notifyPlayer.SendRoomAdd(RoomId);
    }

    public void RemoveFromRoom(Guid sessionId, IServiceChat notifyPlayer)
    {
        sender.Unsubscribe(sessionId);
        notifyPlayer.SendRoomRemove(RoomId);
    }

    public void SendMessage(ChatPlayer player, string message) => _chatService.SendRoomMessage(RoomId, player, message);
    
    public void SendServiceMessage(string message, bool isLocalized, Dictionary<string, string> args) => 
        _chatService.SendServiceMessage(RoomId, message, isLocalized, args);
    
    public void SendServiceMessage(string message, bool isLocalized = false) => 
        _chatService.SendServiceMessage(RoomId, message, isLocalized, new Dictionary<string, string>());

    public void ClearRoom()
    {
        sender.UnsubscribeAll();
    }
}