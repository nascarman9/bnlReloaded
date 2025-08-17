using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.Database;

public interface IRegionServerDatabase
{
    public bool UserConnected(uint userId);
    public void UserUiChanged(uint userId, UiId uiId, float duration);
    public bool RegisterService(Guid sessionId, IService service, ServiceId serviceId);
    public bool RemoveServices(Guid sessionId);
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
}