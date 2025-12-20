using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServicePlayer : IService
{
    public void SendPlayerUpdate(PlayerUpdate playerUpdate);
    public void SendServerRevision(string revision);
    public void SendNotifySquadInvite(uint senderId, string nickname);
    public void SendNotifySquadInviteReply(uint receiverId, SquadInviteReplyType reply, string nickname);
    public void SendNotifySquadInviteCancel(uint senderId);
    public void SendUpdateSquad(SquadUpdate? update);
    public void SendNotifyKickFromSquad();
    public void SendSearchUser(ushort rpcId, List<SearchResult>? data, string? error = null);
    public void SendLookingForFriends(ushort rpcId, string? error = null);
    public void SendRequestProfile(ushort rpcId, ProfileData? profile, string? error = null);
    public void SendGetLoadout(ushort rpcId, LobbyLoadout? loadout, string? error = null);
    public void SendGetAvailableRegions(ushort rpcId, List<string>? regions, string? currentRegion, bool? remember, string? error = null);
    public void SendSwitchRegion(ushort rpcId, bool? accepted, string? error = null);
    public void SendNotification(Notification data);
    public void SendDailyLogin(int days);
    public void SendSteamCurrency(ushort rpcId, string currency);
    public void SendSteamMicroTxnInitSuccess(ushort rpcId, ulong? orderId);
    public void SendSteamMicroTxnInitFailed(ushort rpcId, string error);
    public void SendBuyShopItem(ushort rpcId, bool? accepted, string? error = null);
    public void SendUpgradeDevice(ushort rpcId, string? error = null);
    public void SendOpenCrate(ushort rpcId, List<LootCrateResult>? items, string? error = null);
}