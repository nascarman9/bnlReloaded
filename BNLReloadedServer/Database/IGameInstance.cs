using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ServerTypes;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.Database;

public interface IGameInstance
{
    public void LinkGuidToPlayer(uint userId, Guid guid, Guid regionGuid);
    public void UserEnteredLobby(uint userId);
    // For when players leave via alt+f4
    public void PlayerDisconnected(uint userId);
    // For when players leave via disconnect button
    public void PlayerLeftInstance(uint userId);
    public void SetMap(MapInfo mapInfo, MapData map);
    public void RegisterServices(Guid sessionId, Dictionary<ServiceId, IService> services);
    public void RemoveService(Guid sessionId);
    public void CreateLobby(Key gameModeKey, MapInfo? mapInfo);
    public ChatRoom? GetChatRoom(RoomId roomId);
    public void SwapHero(uint playerId, Key hero);
    public void UpdateDeviceSlot(uint playerId, int slot, Key? deviceKey);
    public void SwapDevices(uint playerId, int slot1, int slot2);
    public void ResetToDefaultDevices(uint playerId);
    public void SelectPerk(uint playerId, Key perkKey);
    public void DeselectPerk(uint playerId, Key perkKey);
    public void SelectSkin(uint playerId, Key skinKey);
    public void SelectRole(uint playerId, PlayerRoleType role);
    public void VoteForMap(uint playerId, Key mapKey);
    public void PlayerReady(uint playerId);
    public void LoadProgressUpdate(uint playerId, float progress);
    public void StartMatch(List<PlayerLobbyState> playerList);
    public void SendUserToZone(uint playerId);
    public void PlayerZoneReady(uint playerId);
    public void UnitMoved(uint unitId, ulong moveTime, ZoneTransform transform);
    public void BuildRequest(ushort rpcId, uint playerId, BuildInfo buildInfo, IServiceZone builderService);
    public void CancelBuildRequest(uint playerId);
    public void EventBroadcast(ZoneEvent zoneEvent);
    public void SwitchGear(ushort rpcId, uint playerId, Key gearKey, IServiceZone switcherService);
    public void StartReload(ushort rpcId, uint playerId, IServiceZone reloaderService);
    public void Reload(ushort rpcId, uint playerId, IServiceZone reloaderService);
    public void ReloadEnd(uint playerId);
    public void ReloadCancel(uint playerId);
    public void CastRequest(uint playerId, CastData castData);
    public void Hit(ulong time, Dictionary<ulong, HitData> hits);

}