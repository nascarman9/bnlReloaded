using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServiceLobby(ISender sender) : IServiceLobby
{
    private enum ServiceLobbyId : byte
    {
        MessageLobbyUpdate = 0,
        MessageClearLobby = 1,
        MessageSwitchHero = 2,
        MessageAddDevice = 3,
        MessageRemoveDevice = 4, 
        MessageSwitchDevice = 5,
        MessageSetDefaultDevices = 6,
        MessageSelectPerk = 7,
        MessageDeselectPerk = 8, 
        MessageSelectSkin = 9,
        MessageSelectRole = 10,
        MessageVoteMap = 11,
        MessagePlayerReady = 12,
        MessageReceiveMatchLoadingProgress = 13,
        MessageSendMatchLoadingProgress = 14,
        MessageRequeueAsIs = 15,
        MessageRequeueAsTeam = 16,
        MessageExitToMenu = 17,
        MessageExitToCustomGame = 18,
        MessageExitToMenuAsSquad = 19
    }
    
    private static BinaryWriter CreateWriter()
    {
        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceLobby);
        return writer;
    }

    public void SendLobbyUpdate(LobbyUpdate update)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLobbyId.MessageLobbyUpdate);
        LobbyUpdate.WriteRecord(writer, update);
        sender.Send(writer);
    }

    public void SendClearLobby()
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLobbyId.MessageClearLobby);
        sender.Send(writer);
    }

    private void ReceiveSwitchHero(BinaryReader reader)
    {
        var heroKey = Key.ReadRecord(reader);
    }

    private void ReceiveAddDevice(BinaryReader reader)
    {
        var deviceKey = Key.ReadRecord(reader);
        var slot = reader.ReadInt32();
    }

    private void ReceiveRemoveDevice(BinaryReader reader)
    {
        var slot = reader.ReadInt32();
    }

    private void ReceiveSwitchDevice(BinaryReader reader)
    {
        var slot1 = reader.ReadInt32();
        var slot2 = reader.ReadInt32();
    }

    private void ReceiveSetDefaultDevices(BinaryReader reader)
    {
        
    }

    private void ReceiveSelectPerk(BinaryReader reader)
    {
        var perkKey = Key.ReadRecord(reader);
    }

    private void ReceiveDeselectPerk(BinaryReader reader)
    {
        var perkKey = Key.ReadRecord(reader);
    }

    private void ReceiveSelectSkin(BinaryReader reader)
    {
        var skinKey = Key.ReadRecord(reader);
    }

    private void ReceiveSelectRole(BinaryReader reader)
    {
        var role = reader.ReadByteEnum<PlayerRoleType>();
    }

    private void ReceiveVoteMap(BinaryReader reader)
    {
        var mapKey = Key.ReadRecord(reader);
    }

    private void ReceivePlayerReady(BinaryReader reader)
    {
        
    }
    
    private void ReceiveMatchLoadingProgress(BinaryReader reader)
    {
        var progress = reader.ReadSingle();
    }

    public void SendMatchLoadingProgress(Dictionary<uint, float> playersProgress)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLobbyId.MessageSendMatchLoadingProgress);
        writer.WriteMap(playersProgress, writer.Write, writer.Write);
        sender.Send(writer);
    }

    private void ReceiveRequeueAsIs(BinaryReader reader)
    {
        
    }

    private void ReceiveRequeueAsTeam(BinaryReader reader)
    {
        
    }

    private void ReceiveExitToMenu(BinaryReader reader)
    {
        
    }

    private void ReceiveExitToCustomGame(BinaryReader reader)
    {
        
    }

    private void ReceiveExitToMenuAsSquad(BinaryReader reader)
    {
        
    }
    
    public void Receive(BinaryReader reader)
    {
        var serviceLobbyId = reader.ReadByte();
        ServiceLobbyId? lobbyEnum = null;
        if (Enum.IsDefined(typeof(ServiceLobbyId), serviceLobbyId))
        {
            lobbyEnum = (ServiceLobbyId)serviceLobbyId;
        }
        switch (lobbyEnum)
        {
            case ServiceLobbyId.MessageSwitchHero:
                ReceiveSwitchHero(reader);
                break;
            case ServiceLobbyId.MessageAddDevice:
                ReceiveAddDevice(reader);
                break;
            case ServiceLobbyId.MessageRemoveDevice:
                ReceiveRemoveDevice(reader);
                break;
            case ServiceLobbyId.MessageSwitchDevice:
                ReceiveSwitchDevice(reader);
                break;
            case ServiceLobbyId.MessageSetDefaultDevices:
                ReceiveSetDefaultDevices(reader);
                break;
            case ServiceLobbyId.MessageSelectPerk:
                ReceiveSelectPerk(reader);
                break;
            case ServiceLobbyId.MessageDeselectPerk:
                ReceiveDeselectPerk(reader);
                break;
            case ServiceLobbyId.MessageSelectSkin:
                ReceiveSelectSkin(reader);
                break;
            case ServiceLobbyId.MessageSelectRole:
                ReceiveSelectRole(reader);
                break;
            case ServiceLobbyId.MessageVoteMap:
                ReceiveVoteMap(reader);
                break;
            case ServiceLobbyId.MessagePlayerReady:
                ReceivePlayerReady(reader);
                break;
            case ServiceLobbyId.MessageReceiveMatchLoadingProgress:
                ReceiveMatchLoadingProgress(reader);
                break;
            case ServiceLobbyId.MessageRequeueAsIs:
                ReceiveRequeueAsIs(reader);
                break;
            case ServiceLobbyId.MessageRequeueAsTeam:
                ReceiveRequeueAsTeam(reader);
                break;
            case ServiceLobbyId.MessageExitToMenu:
                ReceiveExitToMenu(reader);
                break;
            case ServiceLobbyId.MessageExitToCustomGame:
                ReceiveExitToCustomGame(reader);
                break;
            case ServiceLobbyId.MessageExitToMenuAsSquad:
                ReceiveExitToMenuAsSquad(reader);
                break;
            default:
                Console.WriteLine($"Unknown service lobby id {serviceLobbyId}");
                break;
        }
    }
}