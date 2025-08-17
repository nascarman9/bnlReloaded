using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServicePlayer(ISender sender, IServiceScene serviceScene, IServiceTime serviceTime) : IServicePlayer
{
    private enum ServicePlayerId : byte
    {
        MessageUpdateSteamInfo = 0,
        MessagePlayerUpdate = 1,
        MessageClientRevision = 2,
        MessageServerRevision = 3,
        MessageRequestProfile = 28,
        MessageTrackUiAction = 38,
        MessageSteamCurrency = 44
    }
    
    private readonly IPlayerDatabase _playerDatabase = Databases.PlayerDatabase;
    private readonly IRegionServerDatabase _serverDatabase = Databases.RegionServerDatabase;
    
    private static BinaryWriter CreateWriter()
    {
        var memStream =  new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServicePlayer);
        return writer;
    }

    private void ReceiveUpdateSteamInfo(BinaryReader reader)
    {
        var playerSteamInfo = PlayerSteamInfo.ReadRecord(reader);
        serviceTime.SendSetOrigin(DateTime.Now.ToBinary());
        serviceScene.SendServerUpdate(new ServerUpdate
        {
            BuyPlatinumEnabled = false,
            FriendlyEnabled = true,
            MadModeEnabled = true,
            MapEditorEnabled = true,
            PlayButtonEnabled = true,
            RankedEnabled = true,
            TutorialEnabled = true,
            ShopEnabled = true,
            TimeAssaultEnabled = true
        });
        serviceScene.SendChangeScene(Scene.Create(SceneType.MainMenu));
    }

    public void SendPlayerUpdate(PlayerUpdate playerUpdate)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServicePlayerId.MessagePlayerUpdate);
        PlayerUpdate.WriteRecord(writer, playerUpdate);
        sender.Send(writer);
    }

    private void ReceiveClientRevision(BinaryReader reader)
    {
        var clientRevision = reader.ReadString();
        SendServerRevision("952");
    }
    
    public void SendServerRevision(string revision)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageServerRevision);
        writer.Write(revision);
        sender.Send(writer);
    }

    public void SendRequestProfile(ushort rpcId, ProfileData? profile, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageRequestProfile);
        writer.Write(rpcId);
        if (profile != null)
        {
            writer.Write((byte) 0);
            ProfileData.WriteRecord(writer, profile);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveRequestProfile(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var playerId = reader.ReadUInt32();
        var profileData = _playerDatabase.GetPlayerProfile(playerId);
        SendRequestProfile(rpcId, profileData);
    }

    private void ReceiveTrackUiAction(BinaryReader reader)
    {
        var action = reader.ReadByteEnum<UiId>();
        var enter = reader.ReadBoolean();
        var duration = reader.ReadSingle();
        if (enter && sender.AssociatedPlayerId.HasValue)
        {
           _serverDatabase.UserUiChanged(sender.AssociatedPlayerId.Value, action, duration);
        }
    }

    public void SendSteamCurrency(ushort rpcId, string currency)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageSteamCurrency);
        writer.Write(rpcId);
        writer.Write((byte) 0);
        writer.Write(currency);
        sender.Send(writer);
    }

    private void ReceiveSteamCurrency(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        SendSteamCurrency(rpcId, "USD");
    }

    public void Receive(BinaryReader reader)
    {
        var servicePlayerId = reader.ReadByte();
        Console.WriteLine($"ServicePlayerId: {servicePlayerId}");
        switch (servicePlayerId)
        {
            case (byte)ServicePlayerId.MessageUpdateSteamInfo:
                ReceiveUpdateSteamInfo(reader);
                break;
            case (byte)ServicePlayerId.MessagePlayerUpdate:
                break;
            case (byte)ServicePlayerId.MessageClientRevision:
                ReceiveClientRevision(reader);
                break;
            case (byte)ServicePlayerId.MessageRequestProfile:
                ReceiveRequestProfile(reader);
                break;
            case (byte)ServicePlayerId.MessageTrackUiAction:
                ReceiveTrackUiAction(reader);
                break;
            case (byte)ServicePlayerId.MessageSteamCurrency:
                ReceiveSteamCurrency(reader);
                break;
            default:
                Console.WriteLine($"Player service received unsupported serviceId: {servicePlayerId}");
                break;
        }
    }
}