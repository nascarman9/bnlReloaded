using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.Database;

public interface IRegionServerDatabase
{
    public bool UserConnected(uint userId);
    public void UserUiChanged(uint userId, UiId uiId, float duration);
    public bool UpdateScene(uint userId, Scene scene, IServiceScene sceneService);
    public void UserEnterScene(uint userId);
    public Scene GetLastScene(uint userId);
    public bool RegisterService(Guid sessionId, IService service, ServiceId serviceId);
    public bool RegisterMatchService(Guid sessionId, IService service, ServiceId serviceId);
    public void RemoveServices(Guid sessionId);
    public void RemoveMatchServices(Guid sessionId);
    public bool LinkMatchSessionGuidToUser(uint userId, Guid sessionId);
    public bool AddUser(uint userId, Guid sessionId);
    public bool RemoveUser(uint userId);
    public List<CustomGameInfo> GetCustomGames();
    public ulong? AddCustomGame(string name, string password, uint playerId);
    public bool RemoveCustomGame(ulong gameId);
    public CustomGameJoinResult AddToCustomGame(uint playerId, ulong gameId, string password);
    public bool RemoveFromCustomGame(uint playerId);
    public bool KickFromCustomGame(uint playerId, uint kickerId);
    public bool SwitchTeam(uint playerId);
    public bool UpdateCustomSettings(uint playerId, CustomGameSettings settings);
    public CustomGameUpdate? GetFullCustomGameUpdate(uint playerId);
    public bool StartCustomGame(uint playerId, string? signedMap);
    public IGameInstance? GetGameInstance(uint? playerId);
}