using System.Collections.Concurrent;
using System.Timers;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Servers;
using BNLReloadedServer.ServerTypes;
using BNLReloadedServer.Service;
using NetCoreServer;
using Timer = System.Timers.Timer;

namespace BNLReloadedServer.Database;

public class GameInstance : IGameInstance
{
    private class MatchConnectionInfo(Guid guid, Guid regionGuid, TeamType team, bool isSpectator)
    {
        public Guid Guid { get; set; } = guid;
        public Guid RegionGuid { get; set; } = regionGuid;
        public TeamType Team { get; } = team;
        public bool IsSpectator { get; } = isSpectator;
        public ZoneLoadStage LoadStage { get; set; } = ZoneLoadStage.None;
    }
    
    private TcpServer Server { get; }
    
    public string GameInstanceId { get; }

    private IGameInitiator GameInitiator { get; }
    
    public bool IsStarted { get; private set; }
    
    private Key MatchKey { get; set; }
    private MapInfo? MapInfo { get; set; }
    private MapData? MapData { get; set; }

    private GameLobby? Lobby { get; set; }
    
    private GameZone? Zone { get; set; }
    
    private readonly ConcurrentDictionary<uint, MatchConnectionInfo> _connectedUsers = new();
    
    private readonly SessionSender _lobbySender;
    private readonly SessionSender _zoneSender;
    
    private readonly Dictionary<Guid, Dictionary<ServiceId, IService>> _services = new();

    private InstanceChatRooms ChatRooms { get; }
    
    private Timer? _startGameTimer;
    
    private readonly IRegionServerDatabase _serverDatabase = Databases.RegionServerDatabase;

    public GameInstance(TcpServer matchServer, TcpServer regionServer, string gameInstanceId, IGameInitiator gameInitiator)
    {
        Server = matchServer;
        GameInstanceId = gameInstanceId;
        GameInitiator = gameInitiator;
        _lobbySender = new SessionSender(matchServer);
        _zoneSender = new SessionSender(matchServer);
        ChatRooms = CreateChatRooms(regionServer);
    }

    private InstanceChatRooms CreateChatRooms(TcpServer server)
    {
        var lobbyId = Guid.NewGuid().GetHashCode();
        var instanceId = GameInstanceId.GetHashCode();
        var team1Room = new RoomIdTeam
        {
            InstanceId = instanceId,
            LobbyId = lobbyId,
            Team = TeamType.Team1
        };
        var team2Room = new RoomIdTeam
        {
            InstanceId = instanceId,
            LobbyId = lobbyId,
            Team = TeamType.Team2
        };
        var bothRoom = new RoomIdTeam
        {
            InstanceId = instanceId,
            LobbyId = lobbyId,
            Team = TeamType.Neutral
        };

        var team1ChatRoom = new ChatRoom(team1Room, new SessionSender(server));
        var team2ChatRoom = new ChatRoom(team2Room, new SessionSender(server));
        var bothChatRoom = new ChatRoom(bothRoom, new SessionSender(server));
        return new InstanceChatRooms(team1ChatRoom, team2ChatRoom, bothChatRoom);
    }
    
    public void LinkGuidToPlayer(uint userId, Guid guid, Guid regionGuid)
    {
        var playerTeam = GameInitiator.GetTeamForPlayer(userId);
        var isSpectator = GameInitiator.IsPlayerSpectator(userId);
        if (_connectedUsers.TryGetValue(userId, out var connectedUser))
        {
            connectedUser.Guid = guid;
            connectedUser.RegionGuid = regionGuid;
            connectedUser.LoadStage = ZoneLoadStage.None;
        }
        else
        {
            _connectedUsers[userId] = new MatchConnectionInfo(guid, regionGuid, playerTeam, isSpectator);
        }
        if (_services[regionGuid][ServiceId.ServiceChat] is not IServiceChat chatService) return;
        ChatRooms.BothTeamsRoom.AddToRoom(regionGuid, chatService);
        switch (playerTeam)
        {
            case TeamType.Team1:
                ChatRooms.Team1Room.AddToRoom(regionGuid, chatService);
                break;
            case TeamType.Team2:
                ChatRooms.Team2Room.AddToRoom(regionGuid, chatService);
                break;
            case TeamType.Neutral:
            default:
                break;
        }
    }
    
    public void UserEnteredLobby(uint userId)
    {
        if (!_connectedUsers.TryGetValue(userId, out var value) || Lobby == null) return;
        Lobby.EnqueueAction(() => Lobby.AddPlayer(userId, value.Team));
        var playerGuid = value.Guid;
        _lobbySender.Subscribe(playerGuid);
        _services.TryGetValue(playerGuid, out var service);
        if (service?[ServiceId.ServiceLobby] is ServiceLobby lobbyService)
        {
            lobbyService.SendLobbyUpdate(Lobby.GetLobbyUpdate(userId));
        }
    }

    private void RemoveFromChat(MatchConnectionInfo? player)
    {
        if (player == null || _services[player.RegionGuid][ServiceId.ServiceChat] is not IServiceChat chatService) return;
        ChatRooms.BothTeamsRoom.RemoveFromRoom(player.RegionGuid, chatService);
        switch (player.Team)
        {
            case TeamType.Team1:
                ChatRooms.Team1Room.RemoveFromRoom(player.RegionGuid, chatService);
                break;
            case TeamType.Team2:
                ChatRooms.Team2Room.RemoveFromRoom(player.RegionGuid, chatService);
                break;
            case TeamType.Neutral:
            default:
                break;
        }
    }

    public void PlayerDisconnected(uint userId)
    {
        RemoveFromChat(_connectedUsers[userId]);
        Lobby?.EnqueueAction(() => Lobby?.PlayerDisconnected(userId));
    }
    
    public void PlayerLeftInstance(uint userId)
    {
        _connectedUsers.TryRemove(userId, out var player);
        RemoveFromChat(player);
        
        Lobby?.EnqueueAction(() => Lobby?.PlayerLeft(userId));
        Zone?.EnqueueAction(() => Zone?.PlayerLeft(userId));

        _serverDatabase.RemoveFromGameInstance(userId, GameInstanceId);

        if (!_connectedUsers.IsEmpty) return;
        Zone?.GameCanceler.Cancel();
        IsStarted = false;
        Lobby?.Stop();
        Zone?.Stop();
        Lobby = null;
        Zone = null;
        GameInitiator.ClearInstance();
        _serverDatabase.RemoveGameInstance(GameInstanceId);
    }

    public void SetMap(MapInfo mapInfo, MapData map)
    {
        MapInfo = mapInfo;
        MapData = map;
    }

    public void RegisterServices(Guid sessionId, Dictionary<ServiceId, IService> services)
    {
        _services[sessionId] = services;
    }

    public void RemoveService(Guid sessionId)
    {
        var player = _connectedUsers.Select(kv => (kv.Key, kv.Value)).FirstOrDefault(p => p.Value.Guid == sessionId);
        if (player != default)
        {
            PlayerDisconnected(player.Key);
        }
        _services.Remove(sessionId);
        _lobbySender.Unsubscribe(sessionId);
        _zoneSender.Unsubscribe(sessionId);
    }

    public void CreateLobby(Key gameModeKey, MapInfo? mapInfo)
    {
        var gameMode = Databases.Catalogue.GetCard<CardGameMode>(gameModeKey);
        if (gameMode == null) return;
        
        List<MapInfo> maps = [];
        if (mapInfo != null)
        {
            maps.Add(mapInfo);
        }
        else
        {
            var rnd = new Random();
            var mapGrabCount = gameMode.SelectionMapsCount;
            List<Key> mapKeys;
            var defaultMapList = CatalogueHelper.GetCards<CardMap>(CardCategory.Map).Select(map => map.Key).ToArray();
            switch (gameMode.Ranking)
            {
                case GameRankingType.Friendly:
                case GameRankingType.Graveyard:
                    mapKeys = rnd.GetItems(CatalogueHelper.MapList.Friendly?.ToArray() ?? defaultMapList, mapGrabCount).ToList();
                    break;
                case GameRankingType.Ranked:
                    mapKeys = rnd.GetItems(CatalogueHelper.MapList.Ranked?.ToArray() ?? defaultMapList, mapGrabCount).ToList();
                    break;
                case GameRankingType.None:
                default:
                    mapKeys = rnd.GetItems(CatalogueHelper.MapList.Custom?.ToArray() ?? defaultMapList, mapGrabCount).ToList();
                    break;
            }
            maps = mapKeys.Select(MapInfo (key) => new MapInfoCard { MapKey = key }).ToList();
        }
        
        if (MapData != null)
        {
            MatchKey = CatalogueHelper.GetMatch(MapData.Match)?.Key ?? gameMode.MatchMode;
        }
        else
        {
            MatchKey = gameMode.MatchMode;
        }
        
        Lobby = new GameLobby(new ServiceLobby(_lobbySender), this, GameInstanceId, MatchKey, gameModeKey, maps);
    }

    public ChatRoom? GetChatRoom(RoomId roomId)
    {
        return ChatRooms[roomId];
    }

    public void SwapHero(uint playerId, Key hero) => Lobby?.EnqueueAction(() => Lobby?.SwapHero(playerId, hero));

    public void UpdateDeviceSlot(uint playerId, int slot, Key? deviceKey) =>
        Lobby?.EnqueueAction(() => Lobby?.UpdateDeviceSlot(playerId, slot, deviceKey));

    public void SwapDevices(uint playerId, int slot1, int slot2) => Lobby?.EnqueueAction(() => Lobby?.SwapDevices(playerId, slot1, slot2));

    public void ResetToDefaultDevices(uint playerId) => Lobby?.EnqueueAction(() => Lobby?.ResetToDefaultDevices(playerId));

    public void SelectPerk(uint playerId, Key perkKey) => Lobby?.EnqueueAction(() => Lobby?.SelectPerk(playerId, perkKey));

    public void DeselectPerk(uint playerId, Key perkKey) => Lobby?.EnqueueAction(() => Lobby?.DeselectPerk(playerId, perkKey));

    public void SelectSkin(uint playerId, Key skinKey) => Lobby?.EnqueueAction(() => Lobby?.SelectSkin(playerId, skinKey));
    
    public void SelectRole(uint playerId, PlayerRoleType role) => Lobby?.EnqueueAction(() => Lobby?.SelectRole(playerId, role));
    
    public void VoteForMap(uint playerId, Key mapKey) => Lobby?.EnqueueAction(() => Lobby?.VoteForMap(playerId, mapKey));

    public void PlayerReady(uint playerId) => Lobby?.EnqueueAction(() => Lobby?.PlayerReady(playerId));

    public void LoadProgressUpdate(uint playerId, float progress)
    {
        Lobby?.EnqueueAction(() => Lobby?.LoadProgressUpdate(playerId, progress));
        if (!_connectedUsers.TryGetValue(playerId, out var player)) return;
        if (player.LoadStage != ZoneLoadStage.None || progress < 0.64) return;
        player.LoadStage = ZoneLoadStage.InitZone;
        UploadZoneData(playerId, player);
    }

    public void PlayerZoneReady(uint playerId)
    {
        if (!_connectedUsers.TryGetValue(playerId, out var player)) return;
        player.LoadStage = ZoneLoadStage.LoadZone;
        UploadZoneData(playerId, player);
    }

    public void StartMatch(List<PlayerLobbyState> playerList)
    {
        if (MapData == null) return;
        GameInitiator.StartIntoMatch();
        var bufferedSender = new BufferSender();
        Key? mapKey = null;
        if (MapInfo is MapInfoCard mapInfoCard)
        {
            mapKey = mapInfoCard.MapKey;
        }
        
        Zone = new GameZone(new ServiceZone(bufferedSender), new ServiceZone(_zoneSender), bufferedSender, _zoneSender,
            MapData, GameInitiator, playerList, mapKey);
        
        foreach (var playerId in _connectedUsers.Keys)
        {
            SendUserToZone(playerId);
        }

        _startGameTimer = new Timer(TimeSpan.FromMinutes(2));
        _startGameTimer.AutoReset = false;
        _startGameTimer.Elapsed += OnLoadTimerElapsed;
        _startGameTimer.Start();
    }

    private bool TryBeginGame()
    {
        if (_connectedUsers.Values.Any(user => user.LoadStage != ZoneLoadStage.Finished)) return false;
        BeginGame();
        return true;
    }

    private void BeginGame()
    {
        Zone?.BeginBuildPhase();
        IsStarted = true;
    }

    public void SendUserToZone(uint playerId)
    {
        if (!_connectedUsers.TryGetValue(playerId, out var player)) return;
        var scene = new SceneZone
        {
            GameMode = GameInitiator.GetGameMode(),
            MatchKey = MatchKey,
            MyTeam = player.Team,
            IsSpectator = player.IsSpectator,
            IsMapEditor = GameInitiator.IsMapEditor(),
            Restart = false
        };
        _serverDatabase.UpdateScene(playerId, scene);
    }

    private void UploadZoneData(uint playerId, MatchConnectionInfo player)
    {
        var playerGuid = player.Guid;
        if (_services[playerGuid][ServiceId.ServiceZone] is not IServiceZone zoneService) return;
        switch (player.LoadStage)
        {
            case ZoneLoadStage.InitZone:
                Zone?.EnqueueAction(() => Zone?.SendInitializeZone(zoneService));
                break;
            case ZoneLoadStage.LoadZone:
                Zone?.EnqueueAction(() =>
                {
                    var tempBufferedSender = new BufferSender();
                    var tempSessionSender = new SessionSender(Server, playerGuid, Server.FindSession(playerGuid));
                    Zone?.SendLoadZone(new ServiceZone(tempBufferedSender), playerId);
                    tempSessionSender.Send(tempBufferedSender.GetBuffer());
                    player.LoadStage = ZoneLoadStage.Finished;
                    _zoneSender.Subscribe(playerGuid);
                    if (IsStarted)
                    {
                        Zone?.JoinedInProgress(playerId);
                    }
                    else
                    {
                        TryBeginGame();
                    }
                });
                break;
            case ZoneLoadStage.Finished:
                break;
            case ZoneLoadStage.None:
            default:
                break;
        }
    }

    public void UnitMoved(uint unitId, ulong moveTime, ZoneTransform transform) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedMoveRequest(unitId, moveTime, transform));

    public void BuildRequest(ushort rpcId, uint playerId, BuildInfo buildInfo, IServiceZone builderService) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedBuildRequest(rpcId, playerId, buildInfo, builderService));

    public void CancelBuildRequest(uint playerId) => Zone?.EnqueueAction(() => Zone?.ReceivedCancelBuildRequest(playerId));

    public void EventBroadcast(ZoneEvent zoneEvent) => Zone?.EnqueueAction(() => Zone?.ReceivedEventBroadcast(zoneEvent));
    
    public void SwitchGear(ushort rpcId, uint playerId, Key gearKey, IServiceZone switcherService) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedSwitchGearRequest(rpcId, playerId, gearKey, switcherService));

    public void StartReload(ushort rpcId, uint playerId, IServiceZone reloaderService) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedStartReloadRequest(rpcId, playerId, reloaderService));

    public void Reload(ushort rpcId, uint playerId, IServiceZone reloaderService) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedReloadRequest(rpcId, playerId, reloaderService));

    public void ReloadEnd(uint playerId) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedReloadEndRequest(playerId));

    public void ReloadCancel(uint playerId) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedReloadCancelRequest(playerId));

    public void CastRequest(uint playerId, CastData castData) => Zone?.EnqueueAction(() => Zone?.ReceivedCastRequest(playerId, castData));

    public void Hit(ulong time, Dictionary<ulong, HitData> hits) => Zone?.EnqueueAction(() => Zone?.ReceivedHit(time, hits));

    private void OnLoadTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if(_startGameTimer == null) return;
        _startGameTimer.Stop();
        _startGameTimer.Dispose();
        _startGameTimer = null;
        if (IsStarted) return;
        
        Zone?.EnqueueAction(BeginGame);
    }
}