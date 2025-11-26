using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServiceMatchmaker(ISender sender) : IServiceMatchmaker
{
    private enum ServiceMatchmakerId : byte
    {
        MessageEnterQueue = 0,
        MessageLeaveQueue = 1,
        MessageEnableBackfilling = 2,
        MessageEnableNewHeroLeagueProtection = 3,
        MessageConfirmMatch = 4, 
        MessageMatchmakerUpdate = 5,
        MessageQueueLeft = 6,
        MessageGetCustomGamesList = 7,
        MessageJoinCustomGame = 8, 
        MessageSpectateCustomGame = 9,
        MessageCreateCustomGame = 10,
        MessageStartCustomGame = 11,
        MessageBackfillCustomGame = 12,
        MessageLeaveCustomGame = 13,
        MessageApplyCustomGameSettings = 14,
        MessageSwitchTeam = 15,
        MessageKickPlayer = 16,
        MessageStartTimeTrial = 17,
        MessageRestartTimeTrial = 18,
        MessageUpdateCustomGame = 19,
        MessageCustomGamePlayerKicked = 20,
        MessageExitCustomGame = 21,
        MessageRegisterCustomGame = 22,
        MessageJoinCustomGameBySteam = 23
    }
    
    private readonly IRegionServerDatabase _serverDatabase = Databases.RegionServerDatabase;
    
    private static BinaryWriter CreateWriter()
    {
        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceMatchmaker);
        return writer;
    }

    private void ReceiveEnterQueue(BinaryReader reader)
    {
        var gameMode = Key.ReadRecord(reader);
    }

    private void ReceiveLeaveQueue(BinaryReader reader)
    {
        
    }

    private void ReceiveEnableBackfilling(BinaryReader reader)
    {
        var enable = reader.ReadBoolean();
    }

    private void ReceiveEnableNewHeroLeagueProtection(BinaryReader reader)
    {
        var enable = reader.ReadBoolean();
    }

    private void ReceiveConfirmMatch(BinaryReader reader)
    {
        var confirmMatch = reader.ReadBoolean();
    }
    
    public void SendMatchmakerUpdate(MatchmakerUpdate matchmakerUpdate)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceMatchmakerId.MessageMatchmakerUpdate);
        MatchmakerUpdate.WriteRecord(writer, matchmakerUpdate);
        sender.Send(writer);
    }

    public void SendQueueLeft(uint actorId)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceMatchmakerId.MessageQueueLeft);
        writer.Write(actorId);
        sender.Send(writer);
    }

    public void SendCustomGamesList(ushort rpcId, List<CustomGameInfo> customGamesList, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceMatchmakerId.MessageGetCustomGamesList);
        writer.Write(rpcId);
        if (error == null)
        { 
            writer.Write((byte) 0);
            writer.WriteList(customGamesList, CustomGameInfo.WriteRecord);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error);
        }
        sender.Send(writer);
    }

    private void ReceiveCustomGamesList(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        SendCustomGamesList(rpcId, _serverDatabase.GetCustomGames());
    }

    public void SendJoinCustomGame(ushort rpcId, CustomGameJoinResult result, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceMatchmakerId.MessageJoinCustomGame);
        writer.Write(rpcId);
        if (error == null)
        {
            writer.Write((byte) 0);
            writer.WriteByteEnum(result);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error);
        }
        sender.Send(writer);
    }

    private void ReceiveJoinCustomGame(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var gameId = reader.ReadUInt64();
        var password = reader.ReadString();
        if (!sender.AssociatedPlayerId.HasValue) return;
        var enterStatus = _serverDatabase.AddToCustomGame(sender.AssociatedPlayerId.Value, gameId, password);
        SendJoinCustomGame(rpcId, enterStatus);
        if (enterStatus != CustomGameJoinResult.Accepted) return;
        var update = _serverDatabase.GetFullCustomGameUpdate(sender.AssociatedPlayerId.Value);
        if (update != null)
            SendUpdateCustomGame(update);
    }

    public void SendSpectateCustomGame(ushort rpcId, CustomGameSpectateResult result, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceMatchmakerId.MessageSpectateCustomGame);
        writer.Write(rpcId);
        if (error == null)
        {
            writer.Write((byte) 0);
            writer.WriteByteEnum(result);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error);
        }
        sender.Send(writer);
    }

    private void ReceiveSpectateCustomGame(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var gameId = reader.ReadUInt64();
        var password = reader.ReadString();
    }

    private void ReceiveCreateCustomGame(BinaryReader reader)
    {
        var gameName = reader.ReadString();
        var password = reader.ReadString();
        if (sender.AssociatedPlayerId == null) return;
        _serverDatabase.AddCustomGame(gameName, password, sender.AssociatedPlayerId.Value);
    }

    private void ReceiveStartCustomGame(BinaryReader reader)
    {
        var signedMap = reader.ReadOption(reader.ReadString);
        if (sender.AssociatedPlayerId == null) return;
        _serverDatabase.StartCustomGame(sender.AssociatedPlayerId.Value, signedMap);
    }

    private void ReceiveBackfillCustomGame(BinaryReader reader)
    {
        
    }

    private void ReceiveLeaveCustomGame(BinaryReader reader)
    {
        if (sender.AssociatedPlayerId.HasValue)
        {
            _serverDatabase.RemoveFromCustomGame(sender.AssociatedPlayerId.Value);
        }
    }

    private void ReceiveApplyCustomGameSettings(BinaryReader reader)
    {
        var customGameSettings = CustomGameSettings.ReadRecord(reader);
        if(sender.AssociatedPlayerId.HasValue)
            _serverDatabase.UpdateCustomSettings(sender.AssociatedPlayerId.Value, customGameSettings);
    }

    private void ReceiveSwitchTeam(BinaryReader reader)
    {
        if(sender.AssociatedPlayerId.HasValue)
            _serverDatabase.SwitchTeam(sender.AssociatedPlayerId.Value);
    }

    private void ReceiveKickPlayer(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        if(sender.AssociatedPlayerId.HasValue)
            _serverDatabase.KickFromCustomGame(playerId, sender.AssociatedPlayerId.Value);
    }

    private void ReceiveStartTimeTrial(BinaryReader reader)
    {
        
    }

    private void ReceiveRestartTimeTrial(BinaryReader reader)
    {
        
    }
    
    public void SendUpdateCustomGame(CustomGameUpdate update)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceMatchmakerId.MessageUpdateCustomGame);
        CustomGameUpdate.WriteRecord(writer, update);
        sender.Send(writer);
    }

    public void SendCustomGamePlayerKicked(uint playerId)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceMatchmakerId.MessageCustomGamePlayerKicked);
        writer.Write(playerId);
        sender.Send(writer);
    }

    public void SendExitCustomGame()
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceMatchmakerId.MessageExitCustomGame);
        sender.Send(writer);
    }

    private void ReceiveRegisterCustomGame(BinaryReader reader)
    {
        var gameId = reader.ReadUInt64();
    }

    public void SendJoinCustomGameBySteam(ushort rpcId, CustomGameJoinResult result, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceMatchmakerId.MessageJoinCustomGameBySteam);
        writer.Write(rpcId);
        if (error == null)
        {
            writer.Write((byte) 0);
            writer.WriteByteEnum(result);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error);
        }
        sender.Send(writer);
    }

    private void ReceiveJoinCustomGameBySteam(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var gameId = reader.ReadUInt64();
    }
    
    public void Receive(BinaryReader reader)
    {
        var serviceMatchmakerId = reader.ReadByte();
        ServiceMatchmakerId? matchEnum = null;
        if (Enum.IsDefined(typeof(ServiceMatchmakerId), serviceMatchmakerId))
        {
            matchEnum = (ServiceMatchmakerId)serviceMatchmakerId;
        }

        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"ServiceMatchmakerId: {matchEnum.ToString()}");
        }

        switch (matchEnum)
        {
            case ServiceMatchmakerId.MessageEnterQueue:
                ReceiveEnterQueue(reader);
                break;
            case ServiceMatchmakerId.MessageLeaveQueue:
                ReceiveLeaveQueue(reader);
                break;
            case ServiceMatchmakerId.MessageEnableBackfilling:
                ReceiveEnableBackfilling(reader);
                break;
            case ServiceMatchmakerId.MessageEnableNewHeroLeagueProtection:
                ReceiveEnableNewHeroLeagueProtection(reader);
                break;
            case ServiceMatchmakerId.MessageConfirmMatch:
                ReceiveConfirmMatch(reader);
                break;
            case ServiceMatchmakerId.MessageGetCustomGamesList:
                ReceiveCustomGamesList(reader);
                break;
            case ServiceMatchmakerId.MessageJoinCustomGame:
                ReceiveJoinCustomGame(reader);
                break;
            case ServiceMatchmakerId.MessageSpectateCustomGame:
                ReceiveSpectateCustomGame(reader);
                break;
            case ServiceMatchmakerId.MessageCreateCustomGame:
                ReceiveCreateCustomGame(reader);
                break;
            case ServiceMatchmakerId.MessageStartCustomGame:
                ReceiveStartCustomGame(reader);
                break;
            case ServiceMatchmakerId.MessageBackfillCustomGame:
                ReceiveBackfillCustomGame(reader);
                break;
            case ServiceMatchmakerId.MessageLeaveCustomGame:
                ReceiveLeaveCustomGame(reader);
                break;
            case ServiceMatchmakerId.MessageApplyCustomGameSettings:
                ReceiveApplyCustomGameSettings(reader);
                break;
            case ServiceMatchmakerId.MessageSwitchTeam:
                ReceiveSwitchTeam(reader);
                break;
            case ServiceMatchmakerId.MessageKickPlayer:
                ReceiveKickPlayer(reader);
                break;
            case ServiceMatchmakerId.MessageStartTimeTrial:
                ReceiveStartTimeTrial(reader);
                break;
            case ServiceMatchmakerId.MessageRestartTimeTrial:
                ReceiveRestartTimeTrial(reader);
                break;
            case ServiceMatchmakerId.MessageRegisterCustomGame:
                ReceiveRegisterCustomGame(reader);
                break;
            case ServiceMatchmakerId.MessageJoinCustomGameBySteam:
                ReceiveJoinCustomGameBySteam(reader);
                break;
            default:
                Console.WriteLine($"Unknown service matchmaker id {serviceMatchmakerId}");
                break;
        }
    }
}