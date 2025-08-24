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
        MessageSquadInviteSteam = 4,
        MessageSquadInviteSteamAccept = 5,
        MessageSquadInvite = 6,
        MessageNotifySquadInvite = 7,
        MessageSquadInviteReply = 8,
        MessageNotifySquadInviteReply = 9,
        MessageNotifySquadInviteCancel = 10,
        MessageLeaveSquad = 11,
        MessageUpdateSquad = 12,
        MessageKickFromSquad = 13,
        MessageNotifyKickFromSquad = 14,
        MessageSetSquadGameMode = 15,
        MessageSetSquadFinderSettings = 16,
        MessageJoinSquadFinder = 17,
        MessageLeaveSquadFinder = 18,
        MessageSearchUser = 19,
        MessageFriendRequst = 20,
        MessageFriendRequstAccept = 21,
        MessageFriendRemove = 22,
        MessageLookingForFriends = 23,
        MessageReportPlayer = 24,
        MessageReportHack = 25,
        MessageSelectBadge = 26,
        MessageDeselectBadge = 27,
        MessageRequestProfile = 28,
        MessageGetLoadout = 29,
        MessageSetLoadout = 30,
        MessageGetAvailableRegions = 31,
        MessageSwitchRegion = 32,
        MessageVoiceMute = 33,
        MessageNotification = 34,
        MessageNotificationRead = 35,
        MessageSettingsChanged = 36,
        MessageGameForceClosed = 37,
        MessageTrackUiAction = 38,
        MessageTrackTutorialAction = 39,
        MessageTutorialVideo = 40,
        MessageDailyLogin = 41,
        MessageStartTutorial = 42,
        MessageRefuseChallenge = 43,
        MessageSteamCurrency = 44,
        MessageSteamMicroTxnInit = 45,
        MessageSteamMicroTxnResponse = 46,
        MessageBuyShopItem = 47,
        MessageUpgradeDevice = 48,
        MessageReceiveFreeCrate = 49,
        MessageOpenCrate = 50,
        MessageMarkItemAsShown = 51
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
        serviceTime.SendSetOrigin(DateTimeOffset.Now.ToUnixTimeMilliseconds());
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
        if (!sender.AssociatedPlayerId.HasValue) return;
        var scene = _serverDatabase.GetLastScene(sender.AssociatedPlayerId.Value);
        _serverDatabase.UpdateScene(sender.AssociatedPlayerId.Value, scene, serviceScene);
        SendPlayerUpdate(_playerDatabase.GetFullPlayerUpdate(sender.AssociatedPlayerId.Value));
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

    private void ReceiveSquadInviteSteam(BinaryReader reader)
    {
        var gameMode = Key.ReadRecord(reader);
        var receiverId = reader.ReadUInt32();
        var steamLobbyId = reader.ReadUInt64();
    }

    private void ReceiveSquadInviteSteamAccept(BinaryReader reader)
    {
        var steamLobbyId = reader.ReadUInt64();
    }

    private void ReceiveSquadInvite(BinaryReader reader)
    {
        var gameMode = Key.ReadRecord(reader);
        var receiverId = reader.ReadUInt32();
    }

    public void SendNotifySquadInvite(uint senderId, string nickname)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageNotifySquadInvite);
        writer.Write(senderId);
        writer.Write(nickname);
        sender.Send(writer);
    }

    private void ReceiveSquadInviteReply(BinaryReader reader)
    {
        var senderId = reader.ReadUInt32();
        var reply = reader.ReadByteEnum<SquadInviteReplyType>();
    }

    public void SendNotifySquadInviteReply(uint receiverId, SquadInviteReplyType reply, string nickname)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageNotifySquadInviteReply);
        writer.Write(receiverId);
        writer.WriteByteEnum(reply);
        writer.Write(nickname);
        sender.Send(writer);
    }

    public void SendNotifySquadInviteCancel(uint senderId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageNotifySquadInviteCancel);
        writer.Write(senderId);
        sender.Send(writer);
    }

    private void ReceiveLeaveSquad(BinaryReader reader)
    {
        
    }

    public void SendUpdateSquad(SquadUpdate update)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageUpdateSquad);
        SquadUpdate.WriteRecord(writer, update);
        sender.Send(writer);
    }

    private void ReceiveKickFromSquad(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
    }

    public void SendNotifyKickFromSquad()
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageNotifyKickFromSquad);
        sender.Send(writer);
    }

    private void ReceiveSetSquadGameMode(BinaryReader reader)
    {
        var gameMode = Key.ReadRecord(reader);
    }

    private void ReceiveSetSquadFinderSettings(BinaryReader reader)
    {
        var settings = SquadFinderSettings.ReadRecord(reader);
    }

    private void ReceiveJoinSquadFinder(BinaryReader reader)
    {
        
    }

    private void ReceiveLeaveSquadFinder(BinaryReader reader)
    {
        
    }

    public void SendSearchUser(ushort rpcId, List<SearchResult>? data, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageSearchUser);
        writer.Write(rpcId);
        if (data != null)
        {
            writer.Write((byte) 0);
            writer.WriteList(data, SearchResult.WriteRecord);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveSearchUser(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var pattern = reader.ReadString();
    }

    private void ReceiveFriendRequst(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
    }

    private void ReceiveFriendRequstAccept(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var accept = reader.ReadBoolean();
    }

    private void ReceiveFriendRemove(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
    }

    public void SendLookingForFriends(ushort rpcId, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageLookingForFriends);
        writer.Write(rpcId);
        if (error == null)
        {
            writer.Write((byte) 0);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error);
        }
        sender.Send(writer);
    }

    private void ReceiveLookingForFriends(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var enable = reader.ReadBoolean();
    }

    private void ReceiveReportPlayer(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var category = reader.ReadByteEnum<ReportCategory>();
        var additionalInfo = reader.ReadString();
    }

    private void ReceiveReportHack(BinaryReader reader)
    {
        var hackCategory = reader.ReadString();
        var probability = reader.ReadSingle();
    }

    private void ReceiveSelectBadge(BinaryReader reader)
    {
        var badgeKey = Key.ReadRecord(reader);
    }

    private void ReceiveDeselectBadge(BinaryReader reader)
    {
        var badgeType = reader.ReadByteEnum<BadgeType>();
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
    
    public void SendGetLoadout(ushort rpcId, LobbyLoadout? loadout, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageGetLoadout);
        writer.Write(rpcId);
        if (loadout != null)
        {
            writer.Write((byte) 0);
            LobbyLoadout.WriteRecord(writer, loadout);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveGetLoadout(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var hero = Key.ReadRecord(reader);
        if (!sender.AssociatedPlayerId.HasValue) return;
        SendGetLoadout(rpcId, _playerDatabase.GetLoadoutForHero(sender.AssociatedPlayerId.Value, hero));
    }

    private void ReceiveSetLoadout(BinaryReader reader)
    {
        var loadout = LobbyLoadout.ReadRecord(reader);
    }

    public void SendGetAvailableRegions(ushort rpcId, List<string>? regions, string? currentRegion, bool? remember, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageGetAvailableRegions);
        writer.Write(rpcId);
        if (regions != null)
        {
            writer.Write((byte) 0);
            writer.WriteList(regions, writer.Write);
            writer.Write(currentRegion!);
            writer.Write(remember!.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveGetAvailableRegions(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
    }

    public void SendSwitchRegion(ushort rpcId, bool? accepted, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageSwitchRegion);
        writer.Write(rpcId);
        if (accepted != null)
        {
            writer.Write((byte) 0);
            writer.Write(accepted.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveSwitchRegion(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var region = reader.ReadString();
        var remember = reader.ReadBoolean();
    }

    private void ReceiveVoiceMute(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var isMute = reader.ReadBoolean();
    }

    public void SendNotification(Notification data)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageNotification);
        Notification.WriteVariant(writer, data);
        sender.Send(writer);
    }

    private void ReceiveNotificationRead(BinaryReader reader)
    {
        var notificationId = reader.ReadInt32();
    }

    private void ReceiveSettingsChanged(BinaryReader reader)
    {
        var settings = reader.ReadList<SettingInfo, List<SettingInfo>>(SettingInfo.ReadRecord);
    }

    private void ReceiveGameForceClosed(BinaryReader reader)
    {
        
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

    private void ReceiveTrackTutorialAction(BinaryReader reader)
    {
        var start = reader.ReadBoolean();
        var duration = reader.ReadSingle();
        var endCause = reader.ReadByteEnum<TutorialVideoEndCuase>();
    }

    private void ReceiveTutorialVideo(BinaryReader reader)
    {
        
    }
    
    public void SendDailyLogin(int days)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageDailyLogin);
        writer.Write(days);
        sender.Send(writer);
    }

    private void ReceiveStartTutorial(BinaryReader reader)
    {
        
    }

    private void ReceiveRefuseChallenge(BinaryReader reader)
    {
        var challengeId = reader.ReadUInt64();
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
    
    public void SendSteamMicroTxnInitSuccess(ushort rpcId, ulong? orderId)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServicePlayerId.MessageSteamMicroTxnInit);
        writer.Write(rpcId);
        writer.Write((byte) 0);
        writer.WriteOptionValue(orderId, writer.Write);
        sender.Send(writer);
    }

    public void SendSteamMicroTxnInitFailed(ushort rpcId, string error)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServicePlayerId.MessageSteamMicroTxnInit);
        writer.Write(rpcId);
        writer.Write(byte.MaxValue);
        writer.Write(error);
        sender.Send(writer);
    }

    private void ReceiveSteamMicroTxnInit(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var item = Key.ReadRecord(reader);
        var locale = reader.ReadByteEnum<Locale>();
    }

    private void ReceiveSteamMicroTxnResponse(BinaryReader reader)
    {
        var orderId = reader.ReadUInt64();
        var authorized = reader.ReadByte();
    }

    public void SendBuyShopItem(ushort rpcId, bool? accepted, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageBuyShopItem);
        writer.Write(rpcId);
        if (accepted != null)
        {
            writer.Write((byte) 0);
            writer.Write(accepted.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveBuyShopItem(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var item = Key.ReadRecord(reader);
        var isRealPrice = reader.ReadBoolean();
    }

    public void SendUpgradeDevice(ushort rpcId, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageUpgradeDevice);
        writer.Write(rpcId);
        if (error == null)
        {
            writer.Write((byte) 0);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error);
        }
        sender.Send(writer);
    }

    private void ReceiveUpgradeDevice(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var groupKey = Key.ReadRecord(reader);
    }

    private void ReceiveFreeCrate(BinaryReader reader)
    {
        
    }

    public void SendOpenCrate(ushort rpcId, List<LootCrateResult>? items, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePlayerId.MessageOpenCrate);
        writer.Write(rpcId);
        if (items != null)
        {
            writer.Write((byte)0);
            writer.WriteList(items, LootCrateResult.WriteVariant);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveOpenCrate(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var crateKey = Key.ReadRecord(reader);
    }

    private void ReceiveMarkItemAsShown(BinaryReader reader)
    {
        var itemKey = Key.ReadRecord(reader);
    }
    
    public void Receive(BinaryReader reader)
    {
        var servicePlayerId = reader.ReadByte();
        ServicePlayerId? playerEnum = null;
        if (Enum.IsDefined(typeof(ServicePlayerId), servicePlayerId))
        {
            playerEnum = (ServicePlayerId)servicePlayerId;
        }
        Console.WriteLine($"ServicePlayerId: {playerEnum.ToString()}");
        switch (playerEnum)
        {
            case ServicePlayerId.MessageUpdateSteamInfo:
                ReceiveUpdateSteamInfo(reader);
                break;
            case ServicePlayerId.MessageClientRevision:
                ReceiveClientRevision(reader);
                break;
            case ServicePlayerId.MessageSquadInviteSteam:
                ReceiveSquadInviteSteam(reader);
                break;
            case ServicePlayerId.MessageSquadInviteSteamAccept:
                ReceiveSquadInviteSteamAccept(reader);
                break;
            case ServicePlayerId.MessageSquadInvite:
                ReceiveSquadInvite(reader);
                break;
            case ServicePlayerId.MessageSquadInviteReply:
                ReceiveSquadInviteReply(reader);
                break;
            case ServicePlayerId.MessageLeaveSquad:
                ReceiveLeaveSquad(reader);
                break;
            case ServicePlayerId.MessageKickFromSquad:
                ReceiveKickFromSquad(reader);
                break;
            case ServicePlayerId.MessageSetSquadGameMode:
                ReceiveSetSquadGameMode(reader);
                break;
            case ServicePlayerId.MessageSetSquadFinderSettings:
                ReceiveSetSquadFinderSettings(reader);
                break;
            case ServicePlayerId.MessageJoinSquadFinder:
                ReceiveJoinSquadFinder(reader);
                break;
            case ServicePlayerId.MessageLeaveSquadFinder:
                ReceiveLeaveSquadFinder(reader);
                break;
            case ServicePlayerId.MessageSearchUser:
                ReceiveSearchUser(reader);
                break;
            case ServicePlayerId.MessageFriendRequst:
                ReceiveFriendRequst(reader);
                break;
            case ServicePlayerId.MessageFriendRequstAccept:
                ReceiveFriendRequstAccept(reader);
                break;
            case ServicePlayerId.MessageFriendRemove:
                ReceiveFriendRemove(reader);
                break;
            case ServicePlayerId.MessageLookingForFriends:
                ReceiveLookingForFriends(reader);
                break;
            case ServicePlayerId.MessageReportPlayer:
                ReceiveReportPlayer(reader);
                break;
            case ServicePlayerId.MessageReportHack:
                ReceiveReportHack(reader);
                break;
            case ServicePlayerId.MessageSelectBadge:
                ReceiveSelectBadge(reader);
                break;
            case ServicePlayerId.MessageDeselectBadge:
                ReceiveDeselectBadge(reader);
                break;
            case ServicePlayerId.MessageRequestProfile:
                ReceiveRequestProfile(reader);
                break;
            case ServicePlayerId.MessageGetLoadout:
                ReceiveGetLoadout(reader);
                break;
            case ServicePlayerId.MessageSetLoadout:
                ReceiveSetLoadout(reader);
                break;
            case ServicePlayerId.MessageGetAvailableRegions:
                ReceiveGetAvailableRegions(reader);
                break;
            case ServicePlayerId.MessageSwitchRegion:
                ReceiveSwitchRegion(reader);
                break;
            case ServicePlayerId.MessageVoiceMute:
                ReceiveVoiceMute(reader);
                break;
            case ServicePlayerId.MessageNotificationRead:
                ReceiveNotificationRead(reader);
                break;
            case ServicePlayerId.MessageSettingsChanged:
                ReceiveSettingsChanged(reader);
                break;
            case ServicePlayerId.MessageGameForceClosed:
                ReceiveGameForceClosed(reader);
                break;
            case ServicePlayerId.MessageTrackUiAction:
                ReceiveTrackUiAction(reader);
                break;
            case ServicePlayerId.MessageTrackTutorialAction:
                ReceiveTrackTutorialAction(reader);
                break;
            case ServicePlayerId.MessageTutorialVideo:
                ReceiveTutorialVideo(reader);
                break;
            case ServicePlayerId.MessageStartTutorial:
                ReceiveStartTutorial(reader);
                break;
            case ServicePlayerId.MessageRefuseChallenge:
                ReceiveRefuseChallenge(reader);
                break;
            case ServicePlayerId.MessageSteamCurrency:
                ReceiveSteamCurrency(reader);
                break;
            case ServicePlayerId.MessageSteamMicroTxnInit:
                ReceiveSteamMicroTxnInit(reader);
                break;
            case ServicePlayerId.MessageSteamMicroTxnResponse:
                ReceiveSteamMicroTxnResponse(reader);
                break;
            case ServicePlayerId.MessageBuyShopItem:
                ReceiveBuyShopItem(reader);
                break;
            case ServicePlayerId.MessageUpgradeDevice:
                ReceiveUpgradeDevice(reader);
                break;
            case ServicePlayerId.MessageReceiveFreeCrate:
                ReceiveFreeCrate(reader);
                break;
            case ServicePlayerId.MessageOpenCrate:
                ReceiveOpenCrate(reader);
                break;
            case ServicePlayerId.MessageMarkItemAsShown:
                ReceiveMarkItemAsShown(reader);
                break;
            default:
                Console.WriteLine($"Player service received unsupported serviceId: {servicePlayerId}");
                break;
        }
    }
}