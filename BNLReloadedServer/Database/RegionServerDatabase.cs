using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Servers;
using BNLReloadedServer.ServerTypes;
using BNLReloadedServer.Service;
using NetCoreServer;

namespace BNLReloadedServer.Database;

public class RegionServerDatabase(TcpServer server) : IRegionServerDatabase
{
    private class ConnectionInfo(Guid guid, UiId uiId, float uiDuration)
    {
        public Guid Guid { get; } = guid;
        public UiId UiId { get; set; } = uiId;
        public float UiDuration { get; set; } = uiDuration;
        public ulong? CustomGameId { get; set; }
    }
    
    private readonly Dictionary<uint, ConnectionInfo> _connectedUsers = new();
    private readonly Dictionary<Guid, Dictionary<ServiceId, IService>> _services = new();
    private readonly OrderedDictionary<ulong, (CustomGamePlayerGroup, List<TcpSession>)> _customGamePlayerLists = new();
    
    private readonly IPlayerDatabase _playerDatabase = Databases.PlayerDatabase;

    public bool UserConnected(uint userId) => _connectedUsers.ContainsKey(userId);

    public bool AddUser(uint userId, Guid sessionId)
    {
        return _connectedUsers.TryAdd(userId, new ConnectionInfo(sessionId, UiId.Home, 0));
    }

    public bool RemoveUser(uint userId)
    {
        if (!UserConnected(userId)) return false;

        var customId = _connectedUsers[userId].CustomGameId;
        if (customId.HasValue && _customGamePlayerLists.TryGetValue(customId.Value, out var list))
        {
            if (list.Item1.GameInfo.Status == CustomGameStatus.Preparing)
            {
                RemoveFromCustomGame(userId);
            }
        }
        return _connectedUsers.Remove(userId);
    }

    public void UserUiChanged(uint userId, UiId uiId, float duration)
    {
        if (!UserConnected(userId)) return;
        _connectedUsers[userId].UiId = uiId;
        _connectedUsers[userId].UiDuration = duration;
    }

    public bool RegisterService(Guid sessionId, IService service, ServiceId serviceId)
    {
        if (_services.TryGetValue(sessionId, out var value))
            return value.TryAdd(serviceId, service);
        value = new Dictionary<ServiceId, IService>();
        _services.Add(sessionId, value);
        return value.TryAdd(serviceId, service);
    }

    public bool RemoveServices(Guid sessionId)
    {
        return _services.Remove(sessionId);
    }
    
    public List<CustomGameInfo> GetCustomGames()
    {
        return _customGamePlayerLists.Values.Select(x => x.Item1.GameInfo).ToList();
    }

    public ulong? AddCustomGame(string name, string password, uint playerId)
    {
        if (!UserConnected(playerId)) return null;
        var newCustom = CatalogueFactory.CreateCustomGame(name, password,
            Databases.PlayerDatabase.GetPlayerProfile(playerId).Nickname ?? string.Empty);
        var playerSessions = new List<TcpSession> { server.FindSession(_connectedUsers[playerId].Guid) };
        var sender = new SessionSender(playerSessions);
        var matchService = new ServiceMatchmaker(sender);
        var playerGroup = new CustomGamePlayerGroup(matchService) { Password = password, GameInfo = newCustom };
        _connectedUsers[playerId].CustomGameId = newCustom.Id;
        playerGroup.AddPlayer(playerId, true, _playerDatabase.GetPlayerProfile(playerId));
        _customGamePlayerLists.Add(newCustom.Id, (playerGroup, playerSessions));
        return newCustom.Id;
    }

    public bool RemoveCustomGame(ulong gameId)
    {
        if(!_customGamePlayerLists.TryGetValue(gameId, out var list)) return false;
        foreach (var playerId in list.Item1.Players.Select(player => player.Id))
        {
            if(UserConnected(playerId)) 
                _connectedUsers[playerId].CustomGameId = null;
        }
        return _customGamePlayerLists.Remove(gameId);
    }

    public CustomGameJoinResult AddToCustomGame(uint playerId, ulong gameId, string password)
    {
        if (!UserConnected(playerId) || !_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return CustomGameJoinResult.NoSuchGame;
        var playerSession = server.FindSession(_connectedUsers[playerId].Guid);
        if (password != customGame.Item1.Password) return CustomGameJoinResult.WrongPassword;
        if (!customGame.Item1.GameInfo.AllowBackfilling &&
            customGame.Item1.GameInfo.Status != CustomGameStatus.Preparing) return CustomGameJoinResult.GameStarted;
        if (customGame.Item1.Players.Count >= 10) return CustomGameJoinResult.FullTeams;
        customGame.Item2.Add(playerSession);
        if (customGame.Item1.AddPlayer(playerId, false, _playerDatabase.GetPlayerProfile(playerId)))
        {
            _connectedUsers[playerId].CustomGameId = gameId;
            return CustomGameJoinResult.Accepted;
        }

        customGame.Item2.Remove(playerSession);
        return CustomGameJoinResult.FullTeams;
    }

    public bool RemoveFromCustomGame(uint playerId)
    {
        if (!UserConnected(playerId) || !_connectedUsers[playerId].CustomGameId.HasValue) return false;
        var gameId = _connectedUsers[playerId].CustomGameId.Value;
        if (!_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return false;
        if (customGame.Item1.RemovePlayer(playerId))
        {
            if (!_customGamePlayerLists.ContainsKey(gameId) ||
                customGame.Item2.Remove(server.FindSession(_connectedUsers[playerId].Guid)))
            {
                _connectedUsers[playerId].CustomGameId = null;
                var matchService =
                    _services[_connectedUsers[playerId].Guid][ServiceId.ServiceMatchmaker] as IServiceMatchmaker;
                matchService?.SendExitCustomGame();
                return true;
            }
        }
        return false;
    }

    public bool KickFromCustomGame(uint playerId, uint kickerId)
    {
        if (!UserConnected(playerId) || !_connectedUsers[playerId].CustomGameId.HasValue) return false;
        var gameId = _connectedUsers[playerId].CustomGameId.Value;
        if (!_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return false;
        if (customGame.Item1.KickPlayer(playerId, kickerId) &&
            customGame.Item2.Remove(server.FindSession(_connectedUsers[playerId].Guid)))
        {
            _connectedUsers[playerId].CustomGameId = null;
            var matchService =
                _services[_connectedUsers[playerId].Guid][ServiceId.ServiceMatchmaker] as IServiceMatchmaker;
            matchService?.SendExitCustomGame();
            return true;
        }

        return false;
    }

    public bool SwitchTeam(uint playerId)
    {
        if (!UserConnected(playerId) || !_connectedUsers[playerId].CustomGameId.HasValue) return false;
        var gameId = _connectedUsers[playerId].CustomGameId.Value;
        return _customGamePlayerLists.TryGetValue(gameId, out var customGame) && customGame.Item1.SwapTeam(playerId);
    }

    public bool UpdateCustomSettings(uint playerId, CustomGameSettings settings)
    {
        if (!UserConnected(playerId) || !_connectedUsers[playerId].CustomGameId.HasValue) return false;
        var gameId = _connectedUsers[playerId].CustomGameId.Value;
        return _customGamePlayerLists.TryGetValue(gameId, out var customGame) && customGame.Item1.UpdateSettings(playerId, settings);
    }
}