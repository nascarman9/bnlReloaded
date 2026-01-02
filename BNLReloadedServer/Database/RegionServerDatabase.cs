using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;
using BNLReloadedServer.ServerTypes;
using BNLReloadedServer.Service;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moserware.Skills;
using NetCoreServer;

namespace BNLReloadedServer.Database;

public class RegionServerDatabase(AsyncTaskTcpServer server, AsyncTaskTcpServer matchServer) : IRegionServerDatabase
{
    private class ConnectionInfo(Guid guid, ChatPlayer chatInfo, bool isAdmin)
    {
        public Guid Guid { get; set; } = guid;
        
        public Guid? MatchGuid { get; set; }
        public UiId UiId { get; set; } = UiId.Home;
        public float UiDuration { get; set; }
        public ChatPlayer ChatInfo { get; } = chatInfo;
        public Scene? ActiveScene { get; set; }
        public ulong? CustomGameId { get; set; }
        public string? GameInstanceId { get; set; }
        public ulong? SquadId { get; set; }
        public Key? GameModeForSquadInvite { get; set; }
        public bool Online { get; set; } = true;
        public bool IsAdmin { get; } = isAdmin;
    }
    
    private readonly ConcurrentDictionary<uint, ConnectionInfo> _connectedUsers = new();
    
    private readonly Dictionary<Guid, Dictionary<ServiceId, IService>> _services = new();
    
    // Temporary container for services until we can connect the match session guid to a player
    private readonly Dictionary<Guid, Dictionary<ServiceId, IService>> _matchServices = new();
    
    private readonly OrderedDictionary<ulong, (CustomGamePlayerGroup custom, ISender customSender)> _customGamePlayerLists = new();
    
    private readonly ConcurrentDictionary<string, IGameInstance> _gameInstances = new();
    private readonly ConcurrentDictionary<string, MatchmakerInitiator> _matchmakerGames = new();

    private readonly ChatRoom _globalChatRoom = new(new RoomIdGlobal(), new SessionSender(server));
    
    private readonly IPlayerDatabase _playerDatabase = Databases.PlayerDatabase;

    private readonly Matchmaker _matchmaker = new(server);

    private readonly ConcurrentDictionary<ulong, SquadData> _squads = new();
    
    private static ulong _nextSquadId;

    public bool UserConnected(uint userId) => _connectedUsers.ContainsKey(userId);
    private bool UserConnected(uint userId, [MaybeNullWhen(false)] out ConnectionInfo playerInfo) => _connectedUsers.TryGetValue(userId, out playerInfo);

    public int GetOnlinePlayerCount() => _connectedUsers.Values.Count(player => player.Online);

    public Dictionary<string, int> GetQueueCounts() => new()
    {
        { "casual", _matchmaker.GetQueueCount(CatalogueHelper.ModeFriendly.Key) },
        { "ranked", _matchmaker.GetQueueCount(CatalogueHelper.ModeRanked.Key) }
    };

    public Dictionary<string, List<StatusQueuePlayer>> GetQueuePlayers()
    {
        var queueMap = new Dictionary<string, List<StatusQueuePlayer>>();
        var queues = _matchmaker.GetQueues();

        foreach (var (gameMode, players) in queues)
        {
            var queueName = gameMode == CatalogueHelper.ModeFriendly.Key
                ? "casual"
                : gameMode == CatalogueHelper.ModeRanked.Key
                    ? "ranked"
                    : null;

            if (queueName == null)
            {
                continue;
            }

            queueMap[queueName] = players
                .Select(player => new StatusQueuePlayer
                {
                    Id = player.PlayerId,
                    Name = _playerDatabase.GetPlayerName(player.PlayerId),
                    SquadId = player.SquadId,
                    JoinedAt = player.JoinTime.ToUnixTimeMilliseconds()
                })
                .ToList();
        }

        return queueMap;
    }

    public List<StatusCustomGame> GetCustomGameStatuses() =>
        _customGamePlayerLists.Values.Select(entry => new StatusCustomGame
        {
            Id = entry.custom.GameInfo.Id,
            Name = entry.custom.GameInfo.GameName,
            Private = entry.custom.GameInfo.Private,
            Players = entry.custom.Players.Count,
            MaxPlayers = entry.custom.GameInfo.MaxPlayers,
            Status = entry.custom.GameInfo.Status,
            StatusDescription = entry.custom.GameInfo.Status switch
            {
                CustomGameStatus.Preparing => "preparing",
                CustomGameStatus.Lobby => "lobby",
                CustomGameStatus.Match => "in_match",
                _ => "unknown"
            },
            PlayerList = entry.custom.Players.Select(p => new StatusCustomGamePlayer
            {
                Id = p.Id,
                Nickname = p.Nickname,
                Owner = p.Owner,
                Team = p.Team
            }).ToList()
        }).ToList();

    public List<StatusGameStatus> GetActiveGameStatuses() => _gameInstances.Values
        .Select(instance => instance.GetStatusSnapshot(_playerDatabase))
        .Where(status => status != null)
        .Select(status => status!)
        .ToList();

    private bool GetService<TService>(Guid guid, ServiceId id, [MaybeNullWhen(false)] out TService serviceCaller) where TService: IService
    {
        serviceCaller = default;
        if (!_services.TryGetValue(guid, out var service) ||
            !service.TryGetValue(id, out var serviceInstance)) return false;
        
        var res = serviceInstance is TService instance;
        if (res)
        {
            serviceCaller = (TService)serviceInstance;
        }
        return res;
    }

    public bool LinkMatchSessionGuidToUser(uint userId, Guid sessionId)
    {
        if(!UserConnected(userId, out var player)) return false;
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
        if (scene.Type == SceneType.Lobby || (scene.Type == SceneType.Zone && gameInstance.HasLobby()))
        {
            gameInstance.UserEnteredLobby(userId);
        }

        return true;
    }

    public bool AddUser(uint userId, Guid sessionId)
    {
        var result = true;
        if (!UserConnected(userId, out var playerInfo))
        {
            var chatPlayer = new ChatPlayer
            {
                Nickname = _playerDatabase.GetPlayerName(userId),
                PlayerId = userId
            };

            var data = _playerDatabase.GetPlayerDataNoWait(userId);
            
            result = _connectedUsers.TryAdd(userId, new ConnectionInfo(sessionId, chatPlayer, data is { Role: PlayerRole.Admin or PlayerRole.Core }));
        }
        else
        {
            playerInfo.Guid = sessionId;
            playerInfo.Online = true;
        }
        
        /*if (_services[sessionId][ServiceId.ServiceChat] is IServiceChat chatService)
        {
            _globalChatRoom.AddToRoom(sessionId, chatService);
        }*/
        return result;
    }

    public bool RemoveUser(uint userId)
    {
        if (!UserConnected(userId, out var playerInfo)) return false;

        /*var playerGuid = _connectedUsers[userId].Guid;
        if (_services[playerGuid][ServiceId.ServiceChat] is IServiceChat chatService)
        {
            _globalChatRoom.RemoveFromRoom(playerGuid, chatService);
        }*/

        playerInfo.Online = false;
        _matchmaker.RemovePlayer(userId, null);
        var customId = playerInfo.CustomGameId;
        var gameInstanceId = playerInfo.GameInstanceId;
        if (customId.HasValue && _customGamePlayerLists.TryGetValue(customId.Value, out var list))
        {
            if (list.custom.GameInfo.Status != CustomGameStatus.Preparing) return false;
            RemoveFromCustomGame(userId);
        }
        else if (gameInstanceId is not null && _gameInstances.TryGetValue(gameInstanceId, out var gameInstance))
        {
            if (gameInstance.HasLobby() && !gameInstance.IsOver()) return false;
            gameInstance.PlayerLeftInstance(userId, KickReason.MatchQuit);
        }

        return _connectedUsers.TryRemove(userId, out _);
    }

    public bool UpdateChatName(uint userId, string newName)
    {
        if (!UserConnected(userId, out var playerInfo)) return false;

        playerInfo.ChatInfo.Nickname = newName;
        return true;
    }

    public async Task NotifyFriends(uint playerId)
    {
        foreach (var friend in (await _playerDatabase.GetFriends(playerId)).Select(p => p.PlayerId))
        {
            var info = await _playerDatabase.GetFriends(friend);
            if (UserConnected(friend, out var friendInfo) &&
                GetService<IServicePlayer>(friendInfo.Guid, ServiceId.ServicePlayer, out var servicePlayer))
            {
                servicePlayer.SendPlayerUpdate(new PlayerUpdate
                {
                    Friends = info
                });
            }
        }
    }

    public async Task NotifyRequests(uint receiverId, uint senderId)
    {
        var receiverInfo = await _playerDatabase.GetFriendRequestsFor(receiverId);
        var senderInfo = await _playerDatabase.GetFriendRequestsFrom(senderId);
        
        if (UserConnected(receiverId, out var friendInfo) &&
            GetService<IServicePlayer>(friendInfo.Guid, ServiceId.ServicePlayer, out var servicePlayer))
        {
            servicePlayer.SendPlayerUpdate(new PlayerUpdate
            {
                RequestsFromFriends = receiverInfo
            });
        }
        
        if (UserConnected(senderId, out var friendInfo2) &&
            GetService<IServicePlayer>(friendInfo2.Guid, ServiceId.ServicePlayer, out var servicePlayer2))
        {
            servicePlayer2.SendPlayerUpdate(new PlayerUpdate
            {
                RequestsFromMe = senderInfo
            });
        }
    }

    public void UserUiChanged(uint userId, UiId uiId, float duration)
    {
        if (!UserConnected(userId, out var info)) return;
        info.UiId = uiId;
        info.UiDuration = duration;
    }

    public UiId? GetUiId(uint userId)
    {
        if (!UserConnected(userId, out var info)) return null;
        return info.UiId;
    }

    public bool UpdateScene(uint userId, Scene scene, IServiceScene sceneService, bool enterInstance)
    {
        if(!UserConnected(userId, out var info)) return false;
        info.ActiveScene = scene;
        sceneService.SendChangeScene(scene);
        NotifyFriends(userId);
        switch (scene.Type)
        {
            case SceneType.Lobby:
            case SceneType.Zone:
                if (enterInstance)
                    sceneService.SendEnterInstance(Databases.ConfigDatabase.RegionPublicHost(), 28102,
                        _playerDatabase.GetAuthTokenForPlayer(userId));
                break;
            case SceneType.MainMenu when info.GameInstanceId != null:
                if (_gameInstances.TryGetValue(info.GameInstanceId, out var instance))
                {
                    instance.PlayerLeftInstance(userId, KickReason.MatchQuit);
                }
                break;
        }
        return true;
    }

    public bool UpdateScene(uint userId, Scene scene)
    {
        if(!UserConnected(userId, out var playerInfo)) return false;
        var guid = playerInfo.Guid;
        var oldScene = playerInfo.ActiveScene;
        playerInfo.ActiveScene = scene;
        if (scene.Type == SceneType.MainMenu && !playerInfo.Online)
        {
            RemoveUser(userId);
        }
        return GetService<IServiceScene>(guid, ServiceId.ServiceScene, out var sceneService) && UpdateScene(userId, scene, sceneService, oldScene is SceneMainMenu);
    }

    public Scene GetLastScene(uint userId)
    {
        if (!UserConnected(userId, out var playerInfo)) return new SceneMainMenu();
        return playerInfo.GameInstanceId != null ? playerInfo.ActiveScene ?? new SceneMainMenu() : new SceneMainMenu();
    }

    public void UserEnterScene(uint userId)
    {
        if (!UserConnected(userId, out var player)) return;
        var scene = player.ActiveScene;
        if (scene == null) return;
        switch (scene.Type)
        {
            case SceneType.MainMenu:
                break;
            case SceneType.Lobby:
                break;
            case SceneType.Zone:
                if (player.GameInstanceId != null &&
                    _gameInstances.TryGetValue(player.GameInstanceId, out var instance))
                {
                    instance.PlayerEnterScene(userId);
                }

                break;
        }
    }

    public bool RegisterService(Guid sessionId, IService service, ServiceId serviceId)
    {
        if (_services.TryGetValue(sessionId, out var value))
            return value.TryAdd(serviceId, service);
        value = new Dictionary<ServiceId, IService>();
        var res = _services.TryAdd(sessionId, value);
        return res && value.TryAdd(serviceId, service);
    }

    public bool RegisterMatchService(Guid sessionId, IService service, ServiceId serviceId)
    {
        if (_matchServices.TryGetValue(sessionId, out var value))
            return value.TryAdd(serviceId, service);
        value = new Dictionary<ServiceId, IService>();
        var res = _matchServices.TryAdd(sessionId, value);
        return res && value.TryAdd(serviceId, service);
    }

    public void RemoveServices(Guid sessionId) => _services.Remove(sessionId);

    public void RemoveMatchServices(Guid sessionId)
    {
        _matchServices.Remove(sessionId);
        var player = _connectedUsers.Values.FirstOrDefault(p => p.MatchGuid == sessionId);
        if (player?.GameInstanceId == null) return;
        _gameInstances.TryGetValue(player.GameInstanceId, out var gameInstance);
        gameInstance?.RemoveService(sessionId);
        gameInstance?.RemoveService(player.Guid);
    }

    public List<CustomGameInfo> GetCustomGames() =>
        _customGamePlayerLists.Values.Select(x => x.custom.GameInfo).ToList();

    private bool GetCustomGame(uint playerId, [MaybeNullWhen(false)] out CustomGamePlayerGroup custom)
    {
        custom = null;
        if (!UserConnected(playerId, out var info)) return false;
        var playerCustomGame = info.CustomGameId;
        if (playerCustomGame != null && _customGamePlayerLists.TryGetValue(playerCustomGame.Value, out var cust))
        {
            custom = cust.custom;
        }
        else
        {
            custom = null;
        }
        
        return custom != null;
    }

    public ulong? AddCustomGame(string name, string password, uint playerId)
    {
        if (!UserConnected(playerId, out var playerInfo)) return null;
        var newCustom = CatalogueFactory.CreateCustomGame(name, password,
            Databases.PlayerDatabase.GetPlayerProfile(playerId).Nickname ?? string.Empty);
        var playerGuid = playerInfo.Guid;
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
        playerInfo.CustomGameId = newCustom.Id;
        playerGroup.AddPlayer(playerId, true, _playerDatabase.GetPlayerProfile(playerId));
        if (GetService<IServiceChat>(playerGuid, ServiceId.ServiceChat, out var chatService))
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
            if(UserConnected(playerId, out _)) 
                RemoveFromCustomGame(playerId);
        }
        list.custom.Stop();
        return _customGamePlayerLists.Remove(gameId);
    }

    public CustomGameJoinResult AddToCustomGame(uint playerId, ulong gameId, string password)
    {
        if (!UserConnected(playerId, out var playerInfo) || !_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return CustomGameJoinResult.NoSuchGame;
        var playerGuid = playerInfo.Guid;
        if (password != customGame.custom.Password && !playerInfo.IsAdmin) return CustomGameJoinResult.WrongPassword;
        if (!customGame.custom.GameInfo.AllowBackfilling && !playerInfo.IsAdmin &&
            customGame.custom.GameInfo.Status != CustomGameStatus.Preparing) return CustomGameJoinResult.GameStarted;
        if (customGame.custom.Players.Count >= CatalogueHelper.ModeCustom.PlayersPerTeam * 2) return CustomGameJoinResult.FullTeams;
        customGame.customSender.Subscribe(playerGuid);
        if (customGame.custom.AddPlayer(playerId, false, _playerDatabase.GetPlayerProfile(playerId)))
        {
            playerInfo.CustomGameId = gameId;
            if (GetService<IServiceChat>(playerGuid, ServiceId.ServiceChat, out var chatService))
            {
                customGame.custom.ChatRoom.AddToRoom(playerGuid, chatService);
            }
            return CustomGameJoinResult.Accepted;
        }

        customGame.customSender.Unsubscribe(playerGuid);
        return CustomGameJoinResult.FullTeams;
    }

    public bool BackfillCustomGame(uint playerId)
    {
        if (!UserConnected(playerId, out var playerInfo)) return false;
        var customId = playerInfo.CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        if (!_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return false;
        var playerGuid = playerInfo.Guid;
        playerInfo.GameInstanceId = customGame.custom.GameInstanceId;
        
        if (!GetService<IServiceScene>(playerGuid, ServiceId.ServiceScene, out var sceneService)) return false;
        var scene = new SceneLobby
        {
            MyTeam = customGame.custom.GetTeamForPlayer(playerId),
            GameMode = CatalogueHelper.ModeCustom.Key
        };
        UpdateScene(playerId, scene, sceneService, true);
        return true;
    }

    public CustomGameSpectateResult CheckSpectateCustomGame(uint playerId, ulong gameId, string password)
    {
        if (!UserConnected(playerId, out var playerInfo) || !_customGamePlayerLists.TryGetValue(gameId, out var customGame))
            return CustomGameSpectateResult.NoSuchGame;
        
        if (password != customGame.custom.Password && !playerInfo.IsAdmin) return CustomGameSpectateResult.WrongPassword;
        if (customGame.custom.IsMaxSpectators()) return CustomGameSpectateResult.TooManySpectators;
        return customGame.custom.GameInfo.Status switch
        {
            CustomGameStatus.Preparing => CustomGameSpectateResult.GameNotStartedYet,
            CustomGameStatus.Lobby => CustomGameSpectateResult.GameInLobbyYet,
            CustomGameStatus.Match => CustomGameSpectateResult.Accepted,
            _ => CustomGameSpectateResult.NoSuchGame
        };
    }

    public bool SpectateCustomGame(uint playerId, ulong gameId)
    {
        if (!UserConnected(playerId, out var playerInfo) || !_customGamePlayerLists.TryGetValue(gameId, out var customGame) ||
            customGame.custom.GameInstanceId is null ||
            !_gameInstances.TryGetValue(customGame.custom.GameInstanceId, out var instance)) return false;
        
        if (!customGame.custom.AddSpectator(playerId)) return false;
        playerInfo.CustomGameId = gameId;
        playerInfo.GameInstanceId = customGame.custom.GameInstanceId;
        instance.SendUserToZone(playerId);
        return true;
    }

    public bool RemoveFromCustomGame(uint playerId)
    {
        if (!UserConnected(playerId, out var playerInfo)) return false;
        var customId = playerInfo.CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        if (!_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return false;
        var playerGuid = playerInfo.Guid;
        if (GetService<IServiceChat>(playerGuid, ServiceId.ServiceChat, out var chatService))
        {
            customGame.custom.ChatRoom.RemoveFromRoom(playerGuid, chatService);
        }
        
        customGame.custom.RemoveSpectator(playerId);
        customGame.custom.RemovePlayer(playerId);
        customGame.customSender.Unsubscribe(playerGuid);
        
        playerInfo.CustomGameId = null;
        GetService<IServiceMatchmaker>(playerGuid, ServiceId.ServiceMatchmaker, out var matchService);
        matchService?.SendExitCustomGame();
        return true;
    }

    public bool KickFromCustomGame(uint playerId, uint kickerId)
    {
        if (!UserConnected(playerId, out var playerInfo)) return false;
        var customId = playerInfo.CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        if (!_customGamePlayerLists.TryGetValue(gameId, out var customGame)) return false;
        if (!customGame.custom.KickPlayer(playerId, kickerId)) return false;
        var playerGuid = playerInfo.Guid;
        customGame.customSender.Unsubscribe(playerGuid);
        if (GetService<IServiceChat>(playerGuid, ServiceId.ServiceChat, out var chatService))
        {
            customGame.custom.ChatRoom.RemoveFromRoom(playerGuid, chatService);
        }
        playerInfo.CustomGameId = null;
        GetService<IServiceMatchmaker>(playerGuid, ServiceId.ServiceMatchmaker, out var matchService);
        matchService?.SendExitCustomGame();
        return true;
    }

    public bool SwitchTeam(uint playerId)
    {
        if (!UserConnected(playerId, out var playerInfo)) return false;
        var customId = playerInfo.CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        _customGamePlayerLists.TryGetValue(gameId, out var customGame);
        customGame.custom.EnqueueAction(() => customGame.custom.SwapTeam(playerId));
        return true;
    }

    public bool UpdateCustomSettings(uint playerId, CustomGameSettings settings)
    {
        if (!UserConnected(playerId, out var playerInfo)) return false;
        var customId = playerInfo.CustomGameId;
        if (!customId.HasValue) return false;
        var gameId = customId.Value;
        _customGamePlayerLists.TryGetValue(gameId, out var customGame);
        customGame.custom.EnqueueAction(() => customGame.custom.UpdateSettings(playerId, settings));
        return true;
    }

    public CustomGameUpdate? GetFullCustomGameUpdate(uint playerId)
    {
        if (!UserConnected(playerId, out var playerInfo)) return null;
        var customId = playerInfo.CustomGameId;
        if (!customId.HasValue) return null;
        var gameId = customId.Value;
        return _customGamePlayerLists.TryGetValue(gameId, out var customGame) ? customGame.custom.GetCustomGameUpdate() : null;
    }

    public bool StartCustomGame(uint playerId, string? signedMap)
    {
        if (!UserConnected(playerId, out var info)) return false;
        var customId = info.CustomGameId;
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
            map = Databases.MapDatabase.LoadMapData(mapCard.MapKey);
        }
        
        if (map == null || customGame.custom.GameInfo.MapInfo == null) return false;
        if (!customGame.custom.StartIntoLobby(playerId)) return false;

        var gameInstance = new GameInstance(matchServer, server, Guid.NewGuid().ToString(), customGame.custom);
        customGame.custom.GameInstanceId = gameInstance.GameInstanceId;
        
        gameInstance.SetMap(customGame.custom.GameInfo.MapInfo, map);
        gameInstance.CreateLobby(CatalogueHelper.ModeCustom.Key, customGame.custom.GameInfo.MapInfo);
        if (!_gameInstances.TryAdd(gameInstance.GameInstanceId, gameInstance)) return false;
        var playerArray = customGame.custom.Players.ToArray();
        foreach (var player in playerArray)
        {
            if (!UserConnected(player.Id, out var playerInfo)) continue; 
            playerInfo.GameInstanceId = gameInstance.GameInstanceId;
            var guid = playerInfo.Guid;
            if (!GetService<IServiceScene>(guid, ServiceId.ServiceScene, out var sceneService)) continue;
            var scene = new SceneLobby
            {
                MyTeam = player.Team,
                GameMode = CatalogueHelper.ModeCustom.Key
            };
            UpdateScene(player.Id, scene, sceneService, true);
        }
        return true;
    }

    public bool StartMapEditorGame(uint playerId, MapData map, Key heroKey, TeamType team)
    {
        if (!UserConnected(playerId, out var playerInfo)) return false;
        var dummyInitiator = new DummyGameInitiator(CatalogueHelper.ModeCustom, map, team, true);
        var gameInstance = new GameInstance(matchServer, server, Guid.NewGuid().ToString(), dummyInitiator);
        dummyInitiator.GameInstanceId = gameInstance.GameInstanceId;
        gameInstance.SetMap(null, map);
        gameInstance.SetMatchKey(dummyInitiator.MatchCard.Key);
        _gameInstances.TryAdd(gameInstance.GameInstanceId, gameInstance);
        playerInfo.GameInstanceId = gameInstance.GameInstanceId;
        gameInstance.StartMatch([Databases.PlayerDatabase.GetDummyPlayerLobbyInfo(playerId, heroKey, team)]);
        return true;
    }

    public bool StartGameFromMatchmaker(CardGameMode gameMode, List<PlayerQueueData> team1, List<PlayerQueueData> team2)
    {
        var matchInitiator = new MatchmakerInitiator(gameMode, team1, team2);
        var gameInstance = new GameInstance(matchServer, server, Guid.NewGuid().ToString(), matchInitiator);
        matchInitiator.GameInstanceId = gameInstance.GameInstanceId;
        gameInstance.CreateLobby(gameMode.Key, null);
        if (!_matchmakerGames.TryAdd(gameInstance.GameInstanceId, matchInitiator) ||
            !_gameInstances.TryAdd(gameInstance.GameInstanceId, gameInstance)) return false;
        
        foreach (var player in team1)
        {
            if (!UserConnected(player.PlayerId, out var playerInfo)) continue; 
            playerInfo.GameInstanceId = gameInstance.GameInstanceId;
            var guid = playerInfo.Guid;
            if (!GetService<IServiceScene>(guid, ServiceId.ServiceScene, out var sceneService)) continue;
            var scene = new SceneLobby
            {
                MyTeam = TeamType.Team1,
                GameMode = gameMode.Key
            };
            UpdateScene(player.PlayerId, scene, sceneService, true);
        }

        foreach (var player in team2)
        {
            if (!UserConnected(player.PlayerId, out var playerInfo)) continue; 
            playerInfo.GameInstanceId = gameInstance.GameInstanceId;
            var guid = playerInfo.Guid;
            if (!GetService<IServiceScene>(guid, ServiceId.ServiceScene, out var sceneService)) continue;
            var scene = new SceneLobby
            {
                MyTeam = TeamType.Team2,
                GameMode = gameMode.Key
            };
            UpdateScene(player.PlayerId, scene, sceneService, true);
        }
        
        return true;
    }

    public bool BackfillMatchmakerGame(PlayerQueueData player, TeamType team, string gameInstanceId)
    {
        if (!UserConnected(player.PlayerId, out var playerInfo) || !_matchmakerGames.TryGetValue(gameInstanceId, out var initiator) ||
            !_gameInstances.TryGetValue(gameInstanceId, out _)) return false;
        if(!initiator.AddPlayer(player, team)) return false;
        
        playerInfo.GameInstanceId = gameInstanceId;
        var guid = playerInfo.Guid;
        
        if (!GetService<IServiceScene>(guid, ServiceId.ServiceScene, out var sceneService))
        {
            initiator.RemovePlayer(player.PlayerId);
            return false;
        }
        
        var scene = new SceneLobby
        {
            MyTeam = team,
            GameMode = initiator.GetGameMode()
        };
        
        UpdateScene(player.PlayerId, scene, sceneService, true);
        return true;
    }

    private ChatRoom? GetChatRoom(uint playerId, RoomId roomId)
    {
        if (!UserConnected(playerId, out _)) return null;
        return roomId.Type switch
        {
            RoomIdType.Team => GetGameInstance(playerId)?.GetChatRoom(roomId),
            RoomIdType.Squad => GetSquad(playerId, out var squad) 
                ? squad.ChatRoom.RoomId.Equals(roomId)
                    ? squad.ChatRoom
                    : null
                : null,
            RoomIdType.CustomGame => GetCustomGame(playerId, out var custom) 
                ? custom.ChatRoom.RoomId.Equals(roomId)
                    ? custom.ChatRoom 
                    : null 
                : null,
            RoomIdType.Global => _globalChatRoom,
            _ => null
        };
    }

    public bool SendMessage(uint playerId, RoomId roomId, string message)
    {
        var chatRoom = GetChatRoom(playerId, roomId);
        if (chatRoom == null || !UserConnected(playerId, out var playerInfo)) return false;
        chatRoom.SendMessage(playerInfo.ChatInfo, message);
        return true;
    }

    public PrivateMessageFailReason? SendMessage(uint playerId, uint receiver, string message)
    {
        if (!UserConnected(playerId, out var me) || !UserConnected(receiver, out var them))
            return PrivateMessageFailReason.Offline;
        if (them.GameInstanceId != null) return PrivateMessageFailReason.Match;
        if (_playerDatabase.GetIgnoredUsers(receiver).Contains(playerId)) return PrivateMessageFailReason.Ignor;
        var chatPlayerMe = me.ChatInfo;
        var chatPlayerThem = them.ChatInfo;
        if (!GetService<IServiceChat>(me.Guid, ServiceId.ServiceChat, out var myChatService) || 
            !GetService<IServiceChat>(them.Guid, ServiceId.ServiceChat, out var theirChatService)) 
            return PrivateMessageFailReason.Offline;
        
        myChatService.SendPrivateMessage(chatPlayerMe, chatPlayerThem, message);
        theirChatService.SendPrivateMessage(chatPlayerMe, chatPlayerThem, message);
        return null;
    }

    public IGameInstance? GetGameInstance(uint? playerId)
    {
        if (playerId == null || !UserConnected(playerId.Value, out var playerInfo)) return null;
        var playerGameInstance = playerInfo.GameInstanceId;
        if (playerGameInstance == null)
            return null; 
        _gameInstances.TryGetValue(playerGameInstance, out var instance);
        return instance;
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
        if(!UserConnected(playerId, out var playerInfo)) return false;
        if (playerInfo.GameInstanceId != gameInstanceId) return false;
        playerInfo.GameInstanceId = null;
        UpdateScene(playerId, new SceneMainMenu());

        return true;
    }

    public IEnumerable<(Dictionary<uint, Rating> team1, Dictionary<uint, Rating> team2, string instanceId)> GetBackfillNeeded(Key gameModeKey)
    {
        var gameInstances = _gameInstances.Where(g => g.Value.GetGameMode() == gameModeKey && g.Value.NeedsBackfill()).ToList();
        if (gameInstances.Count == 0) yield break;
        
        foreach (var (instanceId, instance) in gameInstances)
        {
            var (team1, team2) = instance.GetTeamRatings();
            yield return (team1, team2, instanceId);
        }
    }

    public int GetActiveGamesCount(Key gameModeKey) => _gameInstances.Count(g => g.Value.GetGameMode() == gameModeKey);

    public void JoinQueue(uint playerId, Key gameModeKey, IServiceMatchmaker serviceMatchmaker)
    {
        if (!UserConnected(playerId, out var playerInfo) || _playerDatabase.GetPlayerDataNoWait(playerId) is not { } playerData ||
            _playerDatabase.IsBanned(playerId)) return;

        if (playerInfo.SquadId is not null && _squads.TryGetValue(playerInfo.SquadId.Value, out var squad))
        {
            foreach (var pId in squad.GetPlayers())
            {
                if (!UserConnected(pId, out var pInfo) || _playerDatabase.GetPlayerDataNoWait(pId) is not { } pData ||
                    _playerDatabase.IsBanned(pId) ||
                    !GetService<IServiceMatchmaker>(pInfo.Guid, ServiceId.ServiceMatchmaker, out var matchmaker))
                    continue;
                
                _matchmaker.AddPlayer(gameModeKey, pId, pInfo.Guid, pData.Rating, pInfo.SquadId, matchmaker);
            }
        }
        else
        {
            _matchmaker.AddPlayer(gameModeKey, playerId, playerInfo.Guid, playerData.Rating, playerInfo.SquadId, serviceMatchmaker);
        }
    }

    public void LeaveQueue(uint playerId, IServiceMatchmaker serviceMatchmaker)
    {
        if (UserConnected(playerId, out var playerInfo) && playerInfo.SquadId is not null &&
            _squads.TryGetValue(playerInfo.SquadId.Value, out var squad))
        {
            var matchServices = new List<IServiceMatchmaker>();
            foreach (var pId in squad.GetPlayers())
            {
                if (!UserConnected(pId, out var pInfo) ||
                    !GetService<IServiceMatchmaker>(pInfo.Guid, ServiceId.ServiceMatchmaker, out var matchmaker))
                    continue;
                
                matchServices.Add(matchmaker);
            }
            _matchmaker.RemoveSquad(playerInfo.SquadId.Value, matchServices);
        }
        else
        {
            _matchmaker.RemovePlayer(playerId, serviceMatchmaker);
        }
    }

    public void EnableBackfilling(uint playerId, bool enable) => _matchmaker.SetDoBackfilling(playerId, enable);

    public void ConfirmMatch(uint playerId, bool confirm, IServiceMatchmaker serviceMatchmaker) =>
        _matchmaker.OnPopAccepted(playerId, confirm, serviceMatchmaker);

    public void ForceStartMatch(uint playerId)
    {
        if (!UserConnected(playerId, out _) ||
            _playerDatabase.GetPlayerDataNoWait(playerId)?.Role is not (PlayerRole.Admin or PlayerRole.Core)) return;
        
        _matchmaker.ForceStartGame(playerId);
    }

    private bool GetSquad(uint playerId, [MaybeNullWhen(false)] out SquadData squad)
    {
        squad = null;
        if (!UserConnected(playerId, out var playerInfo)) return false;
        var squadId = playerInfo.SquadId;
        
        return squadId.HasValue && _squads.TryGetValue(squadId.Value, out squad);
    }

    public ulong? CreateSquad(uint ownerId, List<uint> players)
    {
        if (!UserConnected(ownerId, out var ownerInfo) || ownerInfo.GameModeForSquadInvite is null) return null;
        var squadId = _nextSquadId++;
        var sender = new SessionSender(server);
        var squadUpdater = new ServicePlayer(sender, new ServiceScene(sender), new ServiceTime(sender));
        
        var squadRoom = new RoomIdSquad
        {
            SquadId = squadId
        };
        
        var squad = new SquadData(squadId, ownerInfo.GameModeForSquadInvite.Value, sender, squadUpdater)
        {
            ChatRoom = new ChatRoom(squadRoom, new SessionSender(server))
        };

        ownerInfo.GameModeForSquadInvite = null;
        
        var realPlayers = new List<uint>();
        var realGuids = new List<Guid>();
        var realChatServices = new List<IServiceChat>();
        var realPlayerServices = new List<IServicePlayer>();
        foreach (var playerId in players)
        {
            if (!UserConnected(playerId, out var playerInfo) ||
                !GetService<IServiceChat>(playerInfo.Guid, ServiceId.ServiceChat, out var chatService) ||
                !GetService<IServicePlayer>(playerInfo.Guid, ServiceId.ServicePlayer, out var playerService)) continue;
            
            if (GetService<IServiceMatchmaker>(playerInfo.Guid, ServiceId.ServiceMatchmaker, out var serviceMatchmaker))
            {
                _matchmaker.RemovePlayer(playerId, serviceMatchmaker);
            }
            
            realPlayers.Add(playerId);
            realGuids.Add(playerInfo.Guid);
            realChatServices.Add(chatService);
            realPlayerServices.Add(playerService);
            playerInfo.SquadId = squadId;
        }
        
        squad.AddPlayers(realPlayers, realGuids, realChatServices, realPlayerServices, ownerId);
        _squads.TryAdd(squadId, squad);
        return squadId;
    }

    private void RemoveSquadFromQueue(ulong squadId)
    {
        if(!_squads.TryGetValue(squadId, out var squad)) return;
        var matchmakersServices = new List<IServiceMatchmaker>();
        foreach (var playerId in squad.GetPlayers())
        {
            if (!UserConnected(playerId, out var playerInfo)) continue;
            if (GetService<IServiceMatchmaker>(playerInfo.Guid, ServiceId.ServiceMatchmaker, out var serviceMatchmaker))
            {
                matchmakersServices.Add(serviceMatchmaker);
            }
        }
        
        _matchmaker.RemoveSquad(squadId, matchmakersServices);
    }

    public bool JoinSquad(uint playerId, ulong squadId)
    {
        if (!UserConnected(playerId, out var playerInfo) || !_squads.TryGetValue(squadId, out var squad) ||
            !GetService<IServiceChat>(playerInfo.Guid, ServiceId.ServiceChat, out var chatService) ||
            !GetService<IServicePlayer>(playerInfo.Guid, ServiceId.ServicePlayer, out var playerService)) return false;

        if (GetService<IServiceMatchmaker>(playerInfo.Guid, ServiceId.ServiceMatchmaker, out var serviceMatchmaker))
        {
            _matchmaker.RemovePlayer(playerId, serviceMatchmaker);
        }
        
        RemoveSquadFromQueue(squadId);
        squad.AddPlayer(playerId, playerInfo.Guid, chatService, playerService, false);
        playerInfo.SquadId = squadId;
        return true;
    }

    public bool LeaveSquad(uint playerId)
    {
        if (!UserConnected(playerId, out var playerInfo) || playerInfo.SquadId is null ||
            !_squads.TryGetValue(playerInfo.SquadId.Value, out var squad)) return false;
        
        RemoveSquadFromQueue(playerInfo.SquadId.Value);
        squad.RemovePlayer(playerId);

        if (GetService<IServiceMatchmaker>(playerInfo.Guid, ServiceId.ServiceMatchmaker, out var matchmakerService))
        {
            _matchmaker.RemovePlayer(playerId, matchmakerService);
        }
        return true;
    }

    public bool KickFromSquad(uint playerId, uint kickerId)
    {
        if (!UserConnected(playerId, out var playerInfo) || playerInfo.SquadId is null ||
            !_squads.TryGetValue(playerInfo.SquadId.Value, out var squad) || !squad.IsOwner(kickerId)) return false;
        
        RemoveSquadFromQueue(playerInfo.SquadId.Value);
        squad.RemovePlayer(playerId);
        if (GetService<IServiceMatchmaker>(playerInfo.Guid, ServiceId.ServiceMatchmaker, out var matchmakerService))
        {
            _matchmaker.RemovePlayer(playerId, matchmakerService);
        }

        if (GetService<IServicePlayer>(playerInfo.Guid, ServiceId.ServicePlayer, out var servicePlayer))
        {
            servicePlayer.SendNotifyKickFromSquad();
        }
        return true;
    }

    public bool SendSquadInvite(uint playerId, uint senderId, Key gameModeKey)
    {
        if (!UserConnected(playerId, out var playerInfo) || !UserConnected(senderId, out var senderInfo) ||
            !GetService<IServicePlayer>(playerInfo.Guid, ServiceId.ServicePlayer, out var playerService)) return false;
        
        senderInfo.GameModeForSquadInvite = gameModeKey;
        playerService.SendNotifySquadInvite(senderId, _playerDatabase.GetPlayerName(senderId));
        return true;
    }

    public bool SendSquadInviteReply(uint playerId, uint senderId, SquadInviteReplyType reply)
    {
        if (!UserConnected(playerId, out var playerInfo) ||
            !GetService<IServicePlayer>(playerInfo.Guid, ServiceId.ServicePlayer, out var playerService)) return false;
        playerService.SendNotifySquadInviteReply(playerId, reply, _playerDatabase.GetPlayerName(senderId));

        if (reply is not SquadInviteReplyType.Accepted) return true;
        
        if (playerInfo.SquadId is null)
        {
            CreateSquad(playerId, [senderId, playerId]);
        }
        else
        {
            JoinSquad(senderId, playerInfo.SquadId.Value);
        }
        return true;
    }

    public bool SetSquadGamemode(uint playerId, Key gameModeKey)
    {
        if (!UserConnected(playerId, out var playerInfo) || playerInfo.SquadId is null ||
            !_squads.TryGetValue(playerInfo.SquadId.Value, out var squad) || !squad.IsOwner(playerId)) return false;
        
        squad.ChangeGameMode(gameModeKey);
        return true;
    }

    public void ClearSquadId(uint playerId)
    {
        if (!UserConnected(playerId, out var playerInfo)) return;
        playerInfo.SquadId = null;
    }

    public void CloseSquad(ulong squadId) => _squads.Remove(squadId, out _);
    
    public ulong? GetSquadId(uint playerId) => UserConnected(playerId, out var playerInfo) ? playerInfo.SquadId : null;
    
    public void SendAfkWarning(uint playerId, string gameInstanceId)
    {
        if (_gameInstances.TryGetValue(gameInstanceId, out var gameInstance))
            gameInstance.SendAfkWarning(playerId);
    }

    public void KickForAfk(uint playerId, string gameInstanceId)
    {
        if (_gameInstances.TryGetValue(gameInstanceId, out var gameInstance))
        {
            gameInstance.PlayerLeftInstance(playerId, KickReason.MatchInactivity);
        }
    }
}
