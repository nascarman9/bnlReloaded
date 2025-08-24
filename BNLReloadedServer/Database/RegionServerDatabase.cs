using System.Collections.Concurrent;
using System.Text.Json;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;
using BNLReloadedServer.ServerTypes;
using BNLReloadedServer.Service;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NetCoreServer;

namespace BNLReloadedServer.Database;

public class RegionServerDatabase(TcpServer server, TcpServer matchServer) : IRegionServerDatabase
{
    private class ConnectionInfo(Guid guid, UiId uiId, float uiDuration)
    {
        public Guid Guid { get; set; } = guid;
        
        public Guid? MatchGuid { get; set; }
        public UiId UiId { get; set; } = uiId;
        public float UiDuration { get; set; } = uiDuration;
        public Scene? ActiveScene { get; set; }
        public ulong? CustomGameId { get; set; }
        public string? GameInstanceId { get; set; }
    }
    
    private readonly ConcurrentDictionary<uint, ConnectionInfo> _connectedUsers = new();
    
    private readonly Dictionary<Guid, Dictionary<ServiceId, IService>> _services = new();
    
    // Temporary container for services until we can connect the match session guid to a player
    private readonly Dictionary<Guid, Dictionary<ServiceId, IService>> _matchServices = new();
    
    private readonly OrderedDictionary<ulong, (CustomGamePlayerGroup custom, ConcurrentDictionary<Guid, TcpSession> customSessions)> _customGamePlayerLists = new();
    private readonly ConcurrentDictionary<string, IGameInstance> _gameInstances = new();
    
    private readonly IPlayerDatabase _playerDatabase = Databases.PlayerDatabase;

    public bool UserConnected(uint userId) => _connectedUsers.ContainsKey(userId);

    public bool LinkMatchSessionGuidToUser(uint userId, Guid sessionId)
    {
        if(!UserConnected(userId)) return false;
        var player = _connectedUsers[userId];
        if (player.GameInstanceId == null) return false;
        _gameInstances.TryGetValue(player.GameInstanceId, out var gameInstance);
        if (gameInstance == null) return false;
        _matchServices.Remove(sessionId, out var matchServices);
        if (matchServices == null) return false;
        var scene = player.ActiveScene;
        if (scene == null || scene.Type == SceneType.MainMenu) return false;
        
        player.MatchGuid = sessionId;
        gameInstance.RegisterServices(sessionId, matchServices);
        gameInstance.LinkGuidToPlayer(userId, sessionId);
        switch (scene.Type)
        {
            case SceneType.Lobby:
                gameInstance.UserEnteredLobby(userId);
                break;
            case SceneType.Zone:
                break;
        }
        return true;
    }

    public bool AddUser(uint userId, Guid sessionId)
    {
        if (!UserConnected(userId))
        {
            return _connectedUsers.TryAdd(userId, new ConnectionInfo(sessionId, UiId.Home, 0));
        }

        _connectedUsers[userId].Guid = sessionId;
        return true;
    }

    public bool RemoveUser(uint userId)
    {
        if (!UserConnected(userId)) return false;

        var customId = _connectedUsers[userId].CustomGameId;
        if (!customId.HasValue || !_customGamePlayerLists.TryGetValue(customId.Value, out var list))
            return _connectedUsers.TryRemove(userId, out _);
        if (list.custom.GameInfo.Status != CustomGameStatus.Preparing) return false;
        RemoveFromCustomGame(userId);
        return _connectedUsers.TryRemove(userId, out _);

    }

    public void UserUiChanged(uint userId, UiId uiId, float duration)
    {
        if (!UserConnected(userId)) return;
        _connectedUsers[userId].UiId = uiId;
        _connectedUsers[userId].UiDuration = duration;
    }

    public bool UpdateScene(uint userId, Scene scene, IServiceScene sceneService)
    {
        if(!UserConnected(userId)) return false;
        _connectedUsers[userId].ActiveScene = scene;
        sceneService.SendChangeScene(scene);
        switch (scene.Type)
        {
            case SceneType.Lobby:
            case SceneType.Zone:
                sceneService.SendEnterInstance(server.Address, 28102, _playerDatabase.GetAuthTokenForPlayer(userId));
                break;
            case SceneType.MainMenu:
            default:
                break;
        }
        return true;
    }

    public Scene GetLastScene(uint userId)
    {
        if (!UserConnected(userId)) return new SceneMainMenu();
        return _connectedUsers[userId].ActiveScene ?? new SceneMainMenu();
    }

    public void UserEnterScene(uint userId)
    {
        if (!UserConnected(userId)) return;
        var player = _connectedUsers[userId];
        var scene = player.ActiveScene;
        if (scene == null) return;
        switch (scene.Type)
        {
            case SceneType.MainMenu:
                break;
            case SceneType.Lobby:
                break;
            case SceneType.Zone:
                break;
        }
    }

    public bool RegisterService(Guid sessionId, IService service, ServiceId serviceId)
    {
        if (_services.TryGetValue(sessionId, out var value))
            return value.TryAdd(serviceId, service);
        value = new Dictionary<ServiceId, IService>();
        _services.Add(sessionId, value);
        return value.TryAdd(serviceId, service);
    }

    public bool RegisterMatchService(Guid sessionId, IService service, ServiceId serviceId)
    {
        if (_matchServices.TryGetValue(sessionId, out var value))
            return value.TryAdd(serviceId, service);
        value = new Dictionary<ServiceId, IService>();
        _matchServices.Add(sessionId, value);
        return value.TryAdd(serviceId, service);
    }

    public void RemoveServices(Guid sessionId)
    {
        _services.Remove(sessionId);
    }

    public void RemoveMatchServices(Guid sessionId)
    {
        _matchServices.Remove(sessionId);
        var player = _connectedUsers.Select(kv => kv.Value).FirstOrDefault(p => p.MatchGuid == sessionId);
        if (player?.GameInstanceId == null) return;
        _gameInstances.TryGetValue(player.GameInstanceId, out var gameInstance);
        gameInstance?.RemoveService(sessionId);
    }

    public List<CustomGameInfo> GetCustomGames()
    {
        return _customGamePlayerLists.Values.Select(x => x.custom.GameInfo).ToList();
    }

    public ulong? AddCustomGame(string name, string password, uint playerId)
    {
        if (!UserConnected(playerId)) return null;
        var newCustom = CatalogueFactory.CreateCustomGame(name, password,
            Databases.PlayerDatabase.GetPlayerProfile(playerId).Nickname ?? string.Empty);
        var playerGuid = _connectedUsers[playerId].Guid;
        var playerSessions = new ConcurrentDictionary<Guid, TcpSession>();
        playerSessions.TryAdd(playerGuid, server.FindSession(playerGuid));
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
        foreach (var playerId in list.custom.Players.Select(player => player.Id))
        {
            if(UserConnected(playerId)) 
                _connectedUsers[playerId].CustomGameId = null;
        }
        return _customGamePlayerLists.Remove(gameId);
    }

    public CustomGameJoinResult AddToCustomGame(uint playerId, ulong gameId, string password)
    {
        if (!UserConnected(playerId) || !_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return CustomGameJoinResult.NoSuchGame;
        var playerGuid = _connectedUsers[playerId].Guid;
        var playerSession = server.FindSession(playerGuid);
        if (password != customGame.custom.Password) return CustomGameJoinResult.WrongPassword;
        if (!customGame.custom.GameInfo.AllowBackfilling &&
            customGame.custom.GameInfo.Status != CustomGameStatus.Preparing) return CustomGameJoinResult.GameStarted;
        if (customGame.custom.Players.Count >= 10) return CustomGameJoinResult.FullTeams;
        customGame.customSessions.TryAdd(playerGuid, playerSession);
        if (customGame.custom.AddPlayer(playerId, false, _playerDatabase.GetPlayerProfile(playerId)))
        {
            _connectedUsers[playerId].CustomGameId = gameId;
            return CustomGameJoinResult.Accepted;
        }

        customGame.customSessions.TryRemove(playerGuid, out _);
        return CustomGameJoinResult.FullTeams;
    }

    public bool RemoveFromCustomGame(uint playerId)
    {
        if (!UserConnected(playerId)) return false;
        var customId = _connectedUsers[playerId].CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        if (!_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return false;
        if (customGame.custom.RemovePlayer(playerId))
        {
            if (!_customGamePlayerLists.ContainsKey(gameId) ||
                customGame.customSessions.TryRemove(_connectedUsers[playerId].Guid, out _))
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
        if (!UserConnected(playerId)) return false;
        var customId = _connectedUsers[playerId].CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        if (!_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return false;
        if (customGame.custom.KickPlayer(playerId, kickerId) &&
            customGame.customSessions.TryRemove(_connectedUsers[playerId].Guid, out _))
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
        if (!UserConnected(playerId)) return false;
        var customId = _connectedUsers[playerId].CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        _customGamePlayerLists.TryGetValue(gameId, out var customGame);
        customGame.custom.EnqueueAction(() => customGame.custom.SwapTeam(playerId));
        return true;
    }

    public bool UpdateCustomSettings(uint playerId, CustomGameSettings settings)
    {
        if (!UserConnected(playerId)) return false;
        var customId = _connectedUsers[playerId].CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        _customGamePlayerLists.TryGetValue(gameId, out var customGame);
        customGame.custom.EnqueueAction(() => customGame.custom.UpdateSettings(playerId, settings));
        return true;
    }

    public CustomGameUpdate? GetFullCustomGameUpdate(uint playerId)
    {
        if (!UserConnected(playerId)) return null;
        var customId = _connectedUsers[playerId].CustomGameId;
        if (!customId.HasValue) return null;
        var gameId = customId.Value;
        return _customGamePlayerLists.TryGetValue(gameId, out var customGame) ? customGame.custom.GetCustomGameUpdate() : null;
    }

    public bool StartCustomGame(uint playerId, string? signedMap)
    {
        if (!UserConnected(playerId)) return false;
        var customId = _connectedUsers[playerId].CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        if (!_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return false;
        
        MapData? map = null;
        if (signedMap != null)
        {
            var handler = new JsonWebTokenHandler();
            var jsonWebToken = handler.ReadJsonWebToken(signedMap);
            
            var rawPayload = jsonWebToken.EncodedPayload;
            var mapJson = Base64UrlEncoder.Decode(rawPayload);
            
            map = JsonSerializer.Deserialize<MapData>(mapJson, JsonHelper.DefaultSerializerSettings);
        }
        else if (customGame.custom.GameInfo.MapInfo is MapInfoCard mapCard)
        {
            map = Databases.Catalogue.GetCard<CardMap>(mapCard.MapKey)?.Data;
        }
        
        if (map == null) return false;
        if (!customGame.custom.StartIntoLobby(playerId)) return false;
        
        var gameInstance = new GameInstance(matchServer)
        {
            GameInstanceId = Guid.NewGuid().ToString(), 
            GameInitiator = customGame.custom
        };
        customGame.custom.GameInstanceId = gameInstance.GameInstanceId;
        
        gameInstance.SetMap(map);
        gameInstance.CreateLobby(CatalogueHelper.ModeCustom.Key, customGame.custom.GameInfo.MapInfo);
        _gameInstances.TryAdd(gameInstance.GameInstanceId, gameInstance);
        var playerArray = customGame.custom.Players.ToArray();
        foreach (var player in playerArray)
        {
            var playerInfo = _connectedUsers[player.Id];
            playerInfo.GameInstanceId = gameInstance.GameInstanceId;
            var guid = playerInfo.Guid;
            if (_services[guid][ServiceId.ServiceScene] is not IServiceScene sceneService) continue;
            var scene = new SceneLobby
            {
                MyTeam = player.Team,
                GameMode = CatalogueHelper.ModeCustom.Key
            };
            UpdateScene(player.Id, scene, sceneService);
        }
        return true;
    }

    public IGameInstance? GetGameInstance(uint? playerId)
    {
        if (playerId == null) return null;
        var playerGameInstance = _connectedUsers[playerId.Value].GameInstanceId;
        return playerGameInstance == null ? null : _gameInstances[playerGameInstance];
    }
}