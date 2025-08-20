using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServiceChat(ISender sender) : IServiceChat
{
    private enum ServiceChatId : byte
    {
        MessageIgnores = 0,
        MessageIgnore = 1,
        MessageRoomAdd = 2,
        MessageRoomRemove = 3,
        MessageReceivePrivateMessage = 4, 
        MessageSendPrivateMessage = 5,
        MessagePrivateMessageFailed = 6,
        MessageReceiveRoomMessage = 7,
        MessageSendRoomMessage = 8, 
        MessageSendServiceMessage = 9
    }
    
    private static BinaryWriter CreateWriter()
    {
        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceChat);
        return writer;
    }

    public void SendIgnores(List<ChatPlayer> players)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceChatId.MessageIgnores);
        writer.WriteList(players, ChatPlayer.WriteRecord);
        sender.Send(writer);
    }

    private void ReceiveIgnore(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var ignore = reader.ReadBoolean();
    }

    public void SendRoomAdd(RoomId room)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceChatId.MessageRoomAdd);
        RoomId.WriteVariant(writer, room);
        sender.Send(writer);
    }

    public void SendRoomRemove(RoomId room)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceChatId.MessageRoomRemove);
        RoomId.WriteVariant(writer, room);
        sender.Send(writer);
    }

    public void SendPrivateMessage(ChatPlayer from, ChatPlayer to, string message)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceChatId.MessageSendPrivateMessage);
        ChatPlayer.WriteRecord(writer, from);
        ChatPlayer.WriteRecord(writer, to);
        writer.Write(message);
        sender.Send(writer);
    }

    private void ReceivePrivateMessage(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var message = reader.ReadString();
    }

    public void SendPrivateMessageFailed(uint toId, PrivateMessageFailReason reason)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceChatId.MessagePrivateMessageFailed);
        writer.Write(toId);
        writer.WriteByteEnum(reason);
        sender.Send(writer);
    }

    public void SendRoomMessage(RoomId roomId, ChatPlayer player, string message)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceChatId.MessageSendRoomMessage);
        RoomId.WriteVariant(writer, roomId);
        ChatPlayer.WriteRecord(writer, player);
        writer.Write(message);
        sender.Send(writer);
    }

    private void ReceiveRoomMessage(BinaryReader reader)
    {
        var roomId = RoomId.ReadVariant(reader);
        var message = reader.ReadString();
    }

    public void SendServiceMessage(RoomId? roomId, string message, bool isLocalized, Dictionary<string, string> arguments)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceChatId.MessageSendServiceMessage);
        writer.WriteOption(roomId, item => RoomId.WriteVariant(writer, item));
        writer.Write(message);
        writer.Write(isLocalized);
        writer.WriteMap(arguments, writer.Write, writer.Write);
        sender.Send(writer);
    }
    
    public void Receive(BinaryReader reader)
    {
        var serviceChatId = reader.ReadByte();
        ServiceChatId? chatEnum = null;
        if (Enum.IsDefined(typeof(ServiceChatId), serviceChatId))
        {
            chatEnum = (ServiceChatId)serviceChatId;
        }
        Console.WriteLine($"ServiceChatId: {chatEnum.ToString()}");
        switch (chatEnum)
        {
            case ServiceChatId.MessageIgnore:
                ReceiveIgnore(reader);
                break;
            case ServiceChatId.MessageReceivePrivateMessage:
                ReceivePrivateMessage(reader);
                break;
            case ServiceChatId.MessageReceiveRoomMessage:
                ReceiveRoomMessage(reader);
                break;
            default:
                Console.WriteLine($"Unknown service chat id {serviceChatId}");
                break;
        }
    }
}