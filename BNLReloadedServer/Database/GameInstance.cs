using System.Collections.Concurrent;
using System.Numerics;
using System.Timers;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Servers;
using BNLReloadedServer.ServerTypes;
using BNLReloadedServer.Service;
using Moserware.Skills;
using NetCoreServer;
using Timer = System.Timers.Timer;

namespace BNLReloadedServer.Database;

public class GameInstance : IGameInstance
{
    private class MatchConnectionInfo(Guid guid, Guid regionGuid, TeamType team, ulong? squadId)
    {
        public Guid Guid { get; set; } = guid;
        public Guid RegionGuid { get; set; } = regionGuid;
        public TeamType Team { get; } = team;
        public ZoneLoadStage LoadStage { get; set; } = ZoneLoadStage.None;
        public ulong? SquadId { get; } = squadId;
    }
    
    private AsyncTaskTcpServer Server { get; }
    
    public string GameInstanceId { get; }

    private IGameInitiator GameInitiator { get; }
    
    public bool IsStarted { get; private set; }
    public bool? HasEnded => Zone?.HasEnded;
    
    private Key MatchKey { get; set; }
    private MapInfo? MapInfo { get; set; }
    private MapData? MapData { get; set; }

    private GameLobby? Lobby { get; set; }
    
    private GameZone? Zone { get; set; }

    private readonly ConcurrentBag<Action> _preZoneActions = [];
    
    private readonly ConcurrentDictionary<uint, MatchConnectionInfo> _connectedUsers = new();
    
    private readonly SessionSender _lobbySender;
    private readonly SessionSender _zoneSender;
    
    private readonly Dictionary<Guid, Dictionary<ServiceId, IService>> _services = new();

    private InstanceChatRooms ChatRooms { get; }
    
    private Timer? _startGameTimer;
    
    private readonly IRegionServerDatabase _serverDatabase = Databases.RegionServerDatabase;

    public GameInstance(AsyncTaskTcpServer matchServer, AsyncTaskTcpServer regionServer, string gameInstanceId, IGameInitiator gameInitiator)
    {
        Server = matchServer;
        GameInstanceId = gameInstanceId;
        GameInitiator = gameInitiator;
        _lobbySender = new SessionSender(matchServer);
        _zoneSender = new SessionSender(matchServer);
        ChatRooms = CreateChatRooms(regionServer);
    }

    private InstanceChatRooms CreateChatRooms(AsyncTaskTcpServer server)
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

    public bool HasLobby() => Lobby != null;

    public bool IsOver() => HasEnded is true;

    public void LinkGuidToPlayer(uint userId, Guid guid, Guid regionGuid)
    {
        var playerTeam = GameInitiator.GetTeamForPlayer(userId);
        var squadId = _serverDatabase.GetSquadId(userId);
        if (_connectedUsers.TryGetValue(userId, out var connectedUser))
        {
            connectedUser.Guid = guid;
            connectedUser.RegionGuid = regionGuid;
            connectedUser.LoadStage = ZoneLoadStage.None;
        }
        else
        {
            _connectedUsers[userId] = new MatchConnectionInfo(guid, regionGuid, playerTeam, squadId);
        }
        if (!_services.TryGetValue(regionGuid, out var svc) ||
            !svc.TryGetValue(ServiceId.ServiceChat, out var service) || service is not IServiceChat chatService) return;
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
        if (!GameInitiator.IsPlayerSpectator(userId))
        {
            Lobby.EnqueueAction(() => Lobby.AddPlayer(userId, value.Team, value.SquadId));
            if (IsStarted)
            {
                ChatRooms.BothTeamsRoom.SendServiceMessage(CatalogueStringHelper.OnEnterChat, true, new Dictionary<string, string>
                {
                    { "player_id", userId.ToString() }
                });
            }
        }
        
        var playerGuid = value.Guid;
        _lobbySender.Subscribe(playerGuid);
        _services.TryGetValue(playerGuid, out var services);
        if (services?.TryGetValue(ServiceId.ServiceLobby, out var service) is true && service is IServiceLobby lobbyService)
        {
            lobbyService.SendLobbyUpdate(Lobby.GetLobbyUpdate(userId));
        }
    }

    private void RemoveFromChat(MatchConnectionInfo? player)
    {
        if (player == null || !_services.TryGetValue(player.RegionGuid, out var svc) ||
            !svc.TryGetValue(ServiceId.ServiceChat, out var service) || service is not IServiceChat chatService) return;
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
        if (!_connectedUsers.TryGetValue(userId, out var player)) return;
        RemoveFromChat(player);
        Lobby?.EnqueueAction(() =>
        {
            _lobbySender.Unsubscribe(player.Guid);
            Lobby?.PlayerDisconnected(userId);
        });
        Zone?.EnqueueAction(() =>
        {
            _zoneSender.Unsubscribe(player.Guid);
            Zone?.PlayerDisconnected(userId);
        });

        if (!GameInitiator.IsPlayerSpectator(userId) && IsStarted)
        {
            ChatRooms.BothTeamsRoom.SendServiceMessage(CatalogueStringHelper.OnDisconnected, true, new Dictionary<string, string>
            {
                { "player_id", userId.ToString() }
            });
        }
    }
    
    public void PlayerLeftInstance(uint userId, KickReason reason)
    {
        _connectedUsers.TryRemove(userId, out var player);
        RemoveFromChat(player);
        
        IServiceLobby? serviceLobby = null;
        if (player?.Guid is not null && HasLobby() && _services.TryGetValue(player.Guid, out var services) &&
            services.TryGetValue(ServiceId.ServiceLobby, out var lobby) && lobby is IServiceLobby lobbyService)
        {
            serviceLobby = lobbyService;
        }
        
        Lobby?.EnqueueAction(() =>
        {
            if (player?.Guid is not null)
                _lobbySender.Unsubscribe(player.Guid);
            Lobby?.PlayerLeft(userId, serviceLobby);
        });

        Zone?.EnqueueAction(() =>
        {
            if (player?.Guid is not null)
                _zoneSender.Unsubscribe(player.Guid);
            
            if (Zone?.PlayerLeft(userId, reason) is true)
            {
                ChatRooms.BothTeamsRoom.SendServiceMessage(
                    reason is KickReason.MatchInactivity 
                        ? CatalogueStringHelper.OnInactivity
                        : reason is KickReason.Cheating or KickReason.Admin 
                            ? CatalogueStringHelper.OnKicked 
                            : Zone?.HasEnded is true 
                                ? CatalogueStringHelper.OnLeaveMatch 
                                : CatalogueStringHelper.OnQuit, 
                        true, new Dictionary<string, string>
                        { 
                            { "player_id", userId.ToString() }
                        });
            }
        });

        _serverDatabase.RemoveFromGameInstance(userId, GameInstanceId);

        if (!_connectedUsers.IsEmpty) return;
        Zone?.GameCanceler.Cancel();
        IsStarted = false;
        Lobby?.Stop();
        Zone?.Stop();
        ChatRooms.ClearRooms();
        Lobby = null;
        Zone = null;
        GameInitiator.ClearInstance(GameInstanceId);
        _serverDatabase.RemoveGameInstance(GameInstanceId);
    }

    public void RemoveAllFromSquad(ulong squadId, Action<uint>? onRemove = null)
    {
        foreach (var playerId in _connectedUsers.Where(p => p.Value.SquadId == squadId).Select(k => k.Key).ToList())
        {
            onRemove?.Invoke(playerId);
            PlayerLeftInstance(playerId, KickReason.MatchQuit);
        }
    }

    public void RemoveAllPlayers(Action<uint>? onRemove = null)
    {
        foreach (var playerId in _connectedUsers.Keys)
        {
            onRemove?.Invoke(playerId);
            PlayerLeftInstance(playerId, KickReason.MatchQuit);
        }
    }

    public void NotifyExitToCustom() =>
        ChatRooms.BothTeamsRoom.SendServiceMessage(CatalogueStringHelper.ReturnToCustomHost, true);

    public void SetMap(MapInfo? mapInfo, MapData map)
    {
        MapInfo = mapInfo;
        MapData = map;
    }

    public bool IsMapNull() => MapInfo is null && MapData is null;

    public void SetMatchKey(Key matchKey)
    {
        MatchKey = matchKey;
    }

    public void RegisterServices(Guid sessionId, Dictionary<ServiceId, IService> services)
    {
        _services[sessionId] = services;
    }

    public void RemoveService(Guid sessionId)
    {
        foreach (var (playerId, _) in _connectedUsers.Where(p => p.Value.Guid == sessionId))
        {
            PlayerDisconnected(playerId);
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
            maps = mapKeys.Where(k => k.GetCard<CardMap>() is not null)
                .Select(MapInfo (key) => new MapInfoCard { MapKey = key }).ToList();
        }
        
        if (MapData != null)
        {
            MatchKey = CatalogueHelper.GetMatch(MapData.Match, gameMode.Key)?.Key ?? gameMode.MatchMode;
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

    public Key GetGameMode() => GameInitiator.GetGameMode();


    public bool NeedsBackfill() => GameInitiator.NeedsBackfill();

    public (Dictionary<uint, Rating> team1, Dictionary<uint, Rating> team2) GetTeamRatings() =>
        GameInitiator.GetTeamRatings();

    public void SendAfkWarning(uint playerId) =>
        ChatRooms.BothTeamsRoom.SendServiceMessage("<PLAYER> will be kicked soon if they remain inactive", false,
            new Dictionary<string, string>
            {
                { "player_id", playerId.ToString() }
            });

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

    public void LoadProgressUpdate(uint playerId, float progress) =>
        Lobby?.EnqueueAction(() => Lobby?.LoadProgressUpdate(playerId, progress));

    public void PlayerEnterScene(uint playerId)
    {
        if (!_connectedUsers.TryGetValue(playerId, out var player)) return;
        player.LoadStage = ZoneLoadStage.InitZone;
        UploadZoneData(playerId, player);   
    }

    public void PlayerZoneReady(uint playerId)
    {
        if (!_connectedUsers.TryGetValue(playerId, out var player)) return;
        player.LoadStage = ZoneLoadStage.LoadZone;
        UploadZoneData(playerId, player);
    }

    public StatusGameStatus? GetStatusSnapshot(IPlayerDatabase playerDatabase)
    {
        var modeKey = GameInitiator.GetGameMode();
        var mode = modeKey == CatalogueHelper.ModeFriendly.Key
            ? "casual"
            : modeKey == CatalogueHelper.ModeRanked.Key
                ? "ranked"
                : modeKey == CatalogueHelper.ModeCustom.Key
                    ? "custom"
                    : null;

        if (mode == null)
        {
            return null;
        }

        Dictionary<uint, MatchPlayerStats>? stats = null;
        DateTimeOffset? attackStartTime = null;
        float? finalDurationSeconds = null;

        if (Zone != null)
        {
            var zoneSnapshot = Zone.GetStatusSnapshot();
            stats = zoneSnapshot.stats;
            attackStartTime = zoneSnapshot.attackStartTime;
            finalDurationSeconds = zoneSnapshot.finalDurationSeconds;
        }

        var status = new StatusGameStatus
        {
            Id = GameInstanceId,
            Mode = mode,
            StartedAt = attackStartTime?.ToUnixTimeMilliseconds(),
            MatchDurationSeconds = finalDurationSeconds ?? (attackStartTime.HasValue
                ? (float)(DateTimeOffset.Now - attackStartTime.Value).TotalSeconds
                : null)
        };

        foreach (var (playerId, info) in _connectedUsers)
        {
            MatchPlayerStats? playerStats = null;

            if (stats != null)
            {
                stats.TryGetValue(playerId, out playerStats);
            }
            status.Players.Add(new StatusGamePlayer
            {
                Id = playerId,
                Name = playerDatabase.GetPlayerName(playerId),
                Team = info.Team,
                Kills = playerStats?.Kills ?? 0,
                Deaths = playerStats?.Deaths ?? 0,
                Assists = playerStats?.Assists ?? 0,
                BlocksBuilt = playerStats?.BlocksBuilt ?? 0,
                BlocksDestroyed = playerStats?.BlocksDestroyed ?? 0,
                ResourcesEarned = playerStats?.ResourcesEarned ?? 0
            });
        }

        return status;
    }

    public void StartMatch(ICollection<PlayerLobbyState> playerList)
    {
        if (MapData == null) return;
        GameInitiator.StartIntoMatch();
        foreach (var playerId in playerList.Select(p => p.PlayerId).Distinct().ToList())
        {
            SendUserToZone(playerId);
        }
        
        var bufferedSender = new BufferSender();
        Key? mapKey = null;
        if (MapInfo is MapInfoCard mapInfoCard)
        {
            mapKey = mapInfoCard.MapKey;
        }
        
        Zone = new GameZone(new ServiceZone(bufferedSender), new ServiceZone(_zoneSender), bufferedSender, _zoneSender,
            MapData, GameInitiator, playerList, mapKey);

        while (_preZoneActions.TryTake(out var action))
        {
            Zone.EnqueueAction(action);
        }

        if (HasLobby())
        {
            _startGameTimer = new Timer(TimeSpan.FromMinutes(2));
            _startGameTimer.AutoReset = false;
            _startGameTimer.Elapsed += OnLoadTimerElapsed;
            _startGameTimer.Start();
        }
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
        Lobby?.StartGame();
        IsStarted = true;
    }

    public void SendUserToZone(uint playerId)
    {
        if (Lobby != null && !GameInitiator.IsPlayerSpectator(playerId))
        {
            var playerDatabase = Databases.PlayerDatabase;
            var lobbyData = Lobby.GetPlayerLobbyState(playerId);
            if (lobbyData != null)
            {
                playerDatabase.UpdateLastPlayedHero(playerId, lobbyData.Hero);
                playerDatabase.UpdateLoadout(playerId, lobbyData.Hero, new LobbyLoadout
                {
                    Devices = lobbyData.Devices,
                    HeroKey = lobbyData.Hero,
                    Perks = lobbyData.Perks,
                    SkinKey = lobbyData.SkinKey
                });
            }
        }
        
        var scene = new SceneZone
        {
            GameMode = GameInitiator.GetGameMode(),
            MatchKey = MatchKey,
            MyTeam = GameInitiator.GetTeamForPlayer(playerId),
            IsSpectator = GameInitiator.IsPlayerSpectator(playerId),
            IsMapEditor = GameInitiator.IsMapEditor(),
            Restart = false
        };
        _serverDatabase.UpdateScene(playerId, scene);
    }

    private void UploadZoneData(uint playerId, MatchConnectionInfo player)
    {
        var playerGuid = player.Guid;
        if (!_services.TryGetValue(playerGuid, out var svc) ||
            !svc.TryGetValue(ServiceId.ServiceZone, out var service) || service is not IServiceZone zoneService) return;
        switch (player.LoadStage)
        {
            case ZoneLoadStage.InitZone:
                if (Zone is null)
                {
                    _preZoneActions.Add(() => Zone?.SendInitializeZone(zoneService));
                }
                else
                {
                    Zone.EnqueueAction(() => Zone.SendInitializeZone(zoneService));
                }
                break;
            case ZoneLoadStage.LoadZone:
                Zone?.EnqueueAction(() =>
                {
                    var tempBufferedSender = new BufferSender();
                    var senderTask = Server.FindAsyncSenderTask(playerGuid);
                    if (senderTask == null)
                    {
                        return;
                    }
                    
                    var tempSessionSender = new SessionSender(Server, playerGuid, senderTask);
                    Zone?.SendLoadZone(new ServiceZone(tempBufferedSender), zoneService, playerId);
                    tempBufferedSender.UseBuffer(tempSessionSender.Send);
                    player.LoadStage = ZoneLoadStage.Finished;
                    _zoneSender.Subscribe(playerGuid);
                    if (IsStarted)
                    {
                        if (Zone?.BeginningZoneInitData.Updates is not null &&
                            Zone.BeginningZoneInitData.Updates.Count > 0)
                        {
                            zoneService.SendBlockUpdates(Zone.BeginningZoneInitData.Updates);
                        }
                            
                        Zone?.JoinedInProgress(playerId, zoneService);
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

    public void CreateProj(uint playerId, ulong shotId, ProjectileInfo projInfo) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedProjCreateRequest(shotId, projInfo,
            _connectedUsers.TryGetValue(playerId, out var value) ? value.Guid : null));

    public void MoveProj(ulong shotId, ulong time, ZoneTransform transform) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedProjMoveRequest(shotId, time, transform));

    public void DropProj(ulong shotId) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedProjDropRequest(shotId));

    public void CreateChannel(ushort rpcId, uint playerId, ChannelData channelData, IServiceZone channelService) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedCreateChannelRequest(rpcId, playerId, channelData, channelService));

    public void EndChannel(uint playerId) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedEndChannelRequest(playerId));

    public void ToolChargeStart(uint playerId, byte toolIndex) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedToolChargeStartRequest(playerId, toolIndex));

    public void ToolChargeEnd(ushort rpcId, uint playerId, byte toolIndex, IServiceZone chargeService) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedToolChargeEndRequest(rpcId, playerId, toolIndex, chargeService));

    public void DashChargeStart(uint playerId, byte toolIndex) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedDashChargeStartRequest(playerId, toolIndex));

    public void DashChargeEnd(ushort rpcId, uint playerId, byte toolIndex, IServiceZone dashService) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedDashChargeEndRequest(rpcId, playerId, toolIndex, dashService));

    public void DashCast(uint playerId, byte toolIndex) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedDashCastRequest(playerId, toolIndex));

    public void DashHit(uint playerId, byte toolIndex, HitData hitData) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedDashHitRequest(playerId, toolIndex, hitData));

    public void GroundSlamCast(uint playerId, byte toolIndex) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedGroundSlamCastRequest(playerId, toolIndex));

    public void GroundSlamHit(uint playerId, byte toolIndex, HitData hitData) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedGroundSlamHitRequest(playerId, toolIndex, hitData));

    public void AbilityCast(ushort rpcId, uint playerId, AbilityCastData castData, IServiceZone abilityService) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedAbilityCastRequest(rpcId, playerId, castData, abilityService));

    public void UnitProjectileHit(uint unitId, HitData hitData) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedUnitProjectileHit(unitId, hitData));

    public void SkybeamHit(uint unitId, HitData hitData) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedSkybeamHit(unitId, hitData));

    public void CastRequest(uint playerId, CastData castData) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedCastRequest(playerId, castData));

    public void Hit(ulong time, Dictionary<ulong, HitData> hits) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedHit(time, hits));

    public void Fall(uint unitId, float height, bool force) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedFall(unitId, height, force)); 

    public void Pickup(uint playerId, uint pickupId) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedPickup(playerId, pickupId));

    public void SelectSpawnPoint(uint playerId, uint? spawnId) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedSelectSpawnPoint(playerId, spawnId));

    public void TurretTarget(uint playerId, uint turretId, uint targetId) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedTurretTarget(playerId, turretId, targetId));

    public void TurretAttack(uint playerId, uint turretId, Vector3 shotPos, List<ShotData> shots) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedTurretAttack(playerId, turretId, shotPos, shots));

    public void MortarAttack(uint mortarId, Vector3 shotPos, List<ShotData> shots) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedMortarAttack(mortarId, shotPos, shots));

    public void DrillAttack(uint drillId, Vector3 shotPos, List<ShotData> shots) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedDrillAttack(drillId, shotPos, shots));

    public void UpdateTesla(uint teslaId, uint? targetId, List<uint> teslasInRange) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedUpdateTesla(teslaId, targetId, teslasInRange));

    public void PlayerCommand(uint playerId, Key command) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedPlayerCommand(playerId, command));

    public void StartRecall(uint playerId) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedStartRecallRequest(playerId));

    public void Surrender(ushort rpcId, uint playerId, IServiceZone surrenderService) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedSurrenderRequest(rpcId, playerId, surrenderService));

    public void SurrenderVote(uint playerId, bool accept) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedSurrenderVoteRequest(playerId, accept));

    public void EditorCommand(uint playerId, MapEditorCommand command, bool force) =>
        Zone?.EnqueueAction(() => Zone?.ReceivedEditorCommand(playerId, command, force));

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