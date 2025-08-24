using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.Database;

public interface IGameInstance
{
    public void LinkGuidToPlayer(uint userId, Guid guid);
    public void UserEnteredLobby(uint userId);
    // For when players leave via alt+f4
    public void PlayerDisconnected(uint userId);
    // For when players leave via disconnect button
    public void PlayerLeftInstance(uint userId);
    public void SetMap(MapData map);
    public void RegisterServices(Guid sessionId, Dictionary<ServiceId, IService> services);
    public void RemoveService(Guid sessionId);
    public void CreateLobby(Key gameModeKey, MapInfo? mapInfo);
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

}