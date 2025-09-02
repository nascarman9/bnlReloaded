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
    private class ConnectionInfo(Guid guid, ChatPlayer chatInfo)
    {
        public Guid Guid { get; set; } = guid;
        
        public Guid? MatchGuid { get; set; }
        public UiId UiId { get; set; } = UiId.Home;
        public float UiDuration { get; set; }
        public ChatPlayer ChatInfo { get; } = chatInfo;
        public Scene? ActiveScene { get; set; }
        public ulong? CustomGameId { get; set; }
        public string? GameInstanceId { get; set; }
    }
    
    private readonly ConcurrentDictionary<uint, ConnectionInfo> _connectedUsers = new();
    
    private readonly Dictionary<Guid, Dictionary<ServiceId, IService>> _services = new();
    
    // Temporary container for services until we can connect the match session guid to a player
    private readonly Dictionary<Guid, Dictionary<ServiceId, IService>> _matchServices = new();
    
    private readonly OrderedDictionary<ulong, (CustomGamePlayerGroup custom, ISender customSender)> _customGamePlayerLists = new();
    private readonly ConcurrentDictionary<string, IGameInstance> _gameInstances = new();

    private readonly ChatRoom _globalChatRoom = new(new RoomIdGlobal(), new SessionSender(server));
    
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
        gameInstance.RegisterServices(player.Guid, new Dictionary<ServiceId, IService>(_services[player.Guid].Where(s => s.Key == ServiceId.ServiceChat)));
        gameInstance.LinkGuidToPlayer(userId, sessionId, player.Guid);
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
        var result = true;
        if (!UserConnected(userId))
        {
            var chatPlayer = new ChatPlayer
            {
                Nickname = _playerDatabase.GetPlayerName(userId),
                PlayerId = userId
            };
            
            result = _connectedUsers.TryAdd(userId, new ConnectionInfo(sessionId, chatPlayer));
        }
        else
        {
            _connectedUsers[userId].Guid = sessionId;
        }
        
        /*if (_services[sessionId][ServiceId.ServiceChat] is IServiceChat chatService)
        {
            _globalChatRoom.AddToRoom(sessionId, chatService);
        }*/
        return result;
    }

    public bool RemoveUser(uint userId)
    {
        if (!UserConnected(userId)) return false;

        /*var playerGuid = _connectedUsers[userId].Guid;
        if (_services[playerGuid][ServiceId.ServiceChat] is IServiceChat chatService)
        {
            _globalChatRoom.RemoveFromRoom(playerGuid, chatService);
        }*/

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

    public bool UpdateScene(uint userId, Scene scene, IServiceScene sceneService, bool enterInstance)
    {
        if(!UserConnected(userId)) return false;
        _connectedUsers[userId].ActiveScene = scene;
        sceneService.SendChangeScene(scene);
        switch (scene.Type)
        {
            case SceneType.Lobby:
            case SceneType.Zone:
                if (enterInstance)
                    sceneService.SendEnterInstance(server.Address, 28102, _playerDatabase.GetAuthTokenForPlayer(userId));
                break;
            case SceneType.MainMenu:
            default:
                break;
        }
        return true;
    }

    public bool UpdateScene(uint userId, Scene scene)
    {
        if(!UserConnected(userId)) return false;
        var playerInfo = _connectedUsers[userId];
        var guid = playerInfo.Guid;
        var oldScene = playerInfo.ActiveScene;
        playerInfo.ActiveScene = scene;
        return _services[guid][ServiceId.ServiceScene] is IServiceScene sceneService && UpdateScene(userId, scene, sceneService, oldScene is SceneMainMenu);
    }

    public Scene GetLastScene(uint userId)
    {
        if (!UserConnected(userId)) return new SceneMainMenu();
        return _connectedUsers[userId].GameInstanceId != null ? _connectedUsers[userId].ActiveScene ?? new SceneMainMenu() : new SceneMainMenu();
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
        gameInstance?.RemoveService(player.Guid);
    }

    public List<CustomGameInfo> GetCustomGames()
    {
        return _customGamePlayerLists.Values.Select(x => x.custom.GameInfo).ToList();
    }
    
    private bool GetCustomGame(uint playerId, out CustomGamePlayerGroup? custom)
    {
        var playerCustomGame = _connectedUsers[playerId].CustomGameId;
        custom = playerCustomGame == null ? null : _customGamePlayerLists[playerCustomGame.Value].custom;
        return custom != null;
    }

    public ulong? AddCustomGame(string name, string password, uint playerId)
    {
        if (!UserConnected(playerId)) return null;
        var newCustom = CatalogueFactory.CreateCustomGame(name, password,
            Databases.PlayerDatabase.GetPlayerProfile(playerId).Nickname ?? string.Empty);
        var playerGuid = _connectedUsers[playerId].Guid;
        var sender = new SessionSender(server);
        sender.Subscribe(playerGuid);
        var matchService = new ServiceMatchmaker(sender);
        var customRoom = new RoomIdCustomGame
        {
            CustomGameId = newCustom.Id
        };
        
        var playerGroup = new CustomGamePlayerGroup(matchService)
        {
            Password = password, 
            GameInfo = newCustom,
            ChatRoom = new ChatRoom(customRoom, new SessionSender(server))
        };
        _connectedUsers[playerId].CustomGameId = newCustom.Id;
        playerGroup.AddPlayer(playerId, true, _playerDatabase.GetPlayerProfile(playerId));
        if (_services[playerGuid][ServiceId.ServiceChat] is IServiceChat chatService)
        {
            playerGroup.ChatRoom.AddToRoom(playerGuid, chatService);
        }
        
        _customGamePlayerLists.Add(newCustom.Id, (playerGroup, sender));
        return newCustom.Id;
    }

    public bool RemoveCustomGame(ulong gameId)
    {
        if(!_customGamePlayerLists.TryGetValue(gameId, out var list)) return false;
        foreach (var playerId in list.custom.Players.Select(player => player.Id))
        {
            if(UserConnected(playerId)) 
                RemoveFromCustomGame(playerId);
        }
        list.custom.Stop();
        return _customGamePlayerLists.Remove(gameId);
    }

    public CustomGameJoinResult AddToCustomGame(uint playerId, ulong gameId, string password)
    {
        if (!UserConnected(playerId) || !_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return CustomGameJoinResult.NoSuchGame;
        var playerGuid = _connectedUsers[playerId].Guid;
        if (password != customGame.custom.Password) return CustomGameJoinResult.WrongPassword;
        if (!customGame.custom.GameInfo.AllowBackfilling &&
            customGame.custom.GameInfo.Status != CustomGameStatus.Preparing) return CustomGameJoinResult.GameStarted;
        if (customGame.custom.Players.Count >= 10) return CustomGameJoinResult.FullTeams;
        customGame.customSender.Subscribe(playerGuid);
        if (customGame.custom.AddPlayer(playerId, false, _playerDatabase.GetPlayerProfile(playerId)))
        {
            _connectedUsers[playerId].CustomGameId = gameId;
            if (_services[playerGuid][ServiceId.ServiceChat] is IServiceChat chatService)
            {
                customGame.custom.ChatRoom.AddToRoom(playerGuid, chatService);
            }
            return CustomGameJoinResult.Accepted;
        }

        customGame.customSender.Unsubscribe(playerGuid);
        return CustomGameJoinResult.FullTeams;
    }

    public bool RemoveFromCustomGame(uint playerId)
    {
        if (!UserConnected(playerId)) return false;
        var customId = _connectedUsers[playerId].CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        if (!_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return false;
        var playerGuid = _connectedUsers[playerId].Guid;
        if (_services[playerGuid][ServiceId.ServiceChat] is IServiceChat chatService)
        {
            customGame.custom.ChatRoom.RemoveFromRoom(playerGuid, chatService);
        }

        customGame.custom.RemovePlayer(playerId);
        
        customGame.customSender.Unsubscribe(playerGuid);
        
        _connectedUsers[playerId].CustomGameId = null;
        var matchService =
            _services[playerGuid][ServiceId.ServiceMatchmaker] as IServiceMatchmaker;
        matchService?.SendExitCustomGame();
        return true;
    }

    public bool KickFromCustomGame(uint playerId, uint kickerId)
    {
        if (!UserConnected(playerId)) return false;
        var customId = _connectedUsers[playerId].CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        if (!_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return false;
        if (!customGame.custom.KickPlayer(playerId, kickerId)) return false;
        var playerGuid = _connectedUsers[playerId].Guid;
        customGame.customSender.Unsubscribe(playerGuid);
        if (_services[playerGuid][ServiceId.ServiceChat] is IServiceChat chatService)
        {
            customGame.custom.ChatRoom.RemoveFromRoom(playerGuid, chatService);
        }
        _connectedUsers[playerId].CustomGameId = null;
        var matchService =
            _services[playerGuid][ServiceId.ServiceMatchmaker] as IServiceMatchmaker;
        matchService?.SendExitCustomGame();
        return true;
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
        
        if (map == null || customGame.custom.GameInfo.MapInfo == null) return false;
        if (!customGame.custom.StartIntoLobby(playerId)) return false;

        var gameInstance = new GameInstance(matchServer, server, Guid.NewGuid().ToString(), customGame.custom);
        customGame.custom.GameInstanceId = gameInstance.GameInstanceId;
        
        gameInstance.SetMap(customGame.custom.GameInfo.MapInfo, map);
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
            UpdateScene(player.Id, scene, sceneService, true);
        }
        return true;
    }

    private ChatRoom? GetChatRoom(uint playerId, RoomId roomId)
    {
        if (!UserConnected(playerId)) return null;
        return roomId.Type switch
        {
            RoomIdType.Team => GetGameInstance(playerId)?.GetChatRoom(roomId),
            RoomIdType.Squad => null, // add later
            RoomIdType.CustomGame => GetCustomGame(playerId, out var custom) 
                ? custom?.ChatRoom.RoomId.Equals(roomId) == true
                    ? custom.ChatRoom 
                    : null 
                : null,
            RoomIdType.Global => _globalChatRoom,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public bool SendMessage(uint playerId, RoomId roomId, string message)
    {
        var chatRoom = GetChatRoom(playerId, roomId);
        if (chatRoom == null) return false;
        chatRoom.SendMessage(_connectedUsers[playerId].ChatInfo, message);
        return true;
    }

    public PrivateMessageFailReason? SendMessage(uint playerId, uint receiver, string message)
    {
        if (!UserConnected(playerId) || !UserConnected(receiver)) return PrivateMessageFailReason.Offline;
        if (_connectedUsers[receiver].GameInstanceId != null) return PrivateMessageFailReason.Match;
        if (_playerDatabase.GetIgnoredUsers(receiver).Contains(playerId)) return PrivateMessageFailReason.Ignor;
        var me = _connectedUsers[playerId];
        var them = _connectedUsers[receiver];
        var chatPlayerMe = me.ChatInfo;
        var chatPlayerThem = them.ChatInfo;
        if (_services[me.Guid][ServiceId.ServiceChat] is not IServiceChat myChatService || 
            _services[them.Guid][ServiceId.ServiceChat] is not IServiceChat theirChatService) 
            return PrivateMessageFailReason.Offline;
        
        myChatService.SendPrivateMessage(chatPlayerMe, chatPlayerThem, message);
        theirChatService.SendPrivateMessage(chatPlayerMe, chatPlayerThem, message);
        return null;
    }

    public IGameInstance? GetGameInstance(uint? playerId)
    {
        if (playerId == null) return null;
        var playerGameInstance = _connectedUsers[playerId.Value].GameInstanceId;
        return playerGameInstance == null ? null : _gameInstances[playerGameInstance];
    }

    public bool RemoveGameInstance(string gameInstanceId)
    {
        foreach (var playerId in _connectedUsers.Keys)
        {
            RemoveFromGameInstance(playerId, gameInstanceId);
        }
        return _gameInstances.TryRemove(gameInstanceId, out _);
    }

    public bool RemoveFromGameInstance(uint playerId, string gameInstanceId)
    {
        if(!UserConnected(playerId)) return false;
        if (_connectedUsers[playerId].GameInstanceId != gameInstanceId) return false;
        _connectedUsers[playerId].GameInstanceId = null;
        UpdateScene(playerId, new SceneMainMenu());

        return true;
    }
}