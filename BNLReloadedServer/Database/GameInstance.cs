using System.Collections.Concurrent;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Servers;
using BNLReloadedServer.ServerTypes;
using BNLReloadedServer.Service;
using NetCoreServer;

namespace BNLReloadedServer.Database;

public class GameInstance : IGameInstance
{
    private class MatchConnectionInfo(Guid guid, Guid regionGuid, TeamType team)
    {
        public Guid Guid { get; } = guid;
        public Guid RegionGuid { get; } = regionGuid;
        public TeamType Team { get; } = team;
    }
    
    private TcpServer Server { get; }
    
    public string GameInstanceId { get; }

    private IGameInitiator GameInitiator { get; }

    private MapData? MapData { get; set; }

    private GameLobby? Lobby { get; set; }
    
    private readonly ConcurrentDictionary<uint, MatchConnectionInfo> _connectedUsers = new();
    
    private readonly SessionSender _lobbySender;
    private readonly SessionSender _zoneSender;
    
    private readonly Dictionary<Guid, Dictionary<ServiceId, IService>> _services = new();

    private InstanceChatRooms ChatRooms { get; }

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
        _connectedUsers[userId] = new MatchConnectionInfo(guid, regionGuid, playerTeam);
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
            lobbyService.SendLobbyUpdate(Lobby.GetLobbyUpdate());
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
    }

    public void SetMap(MapData map)
    {
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
        
        Lobby = new GameLobby(new ServiceLobby(_lobbySender));
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

        Key matchKey;
        if (MapData != null)
        {
            matchKey = CatalogueHelper.GetMatch(MapData.Match)?.Key ?? gameMode.MatchMode;
        }
        else
        {
            matchKey = gameMode.MatchMode;
        }
        
        Lobby.CreateLobbyData(GameInstanceId, matchKey, gameModeKey, maps);
    }

    public ChatRoom? GetChatRoom(RoomId roomId)
    {
        return ChatRooms[roomId];
    }

    public void SwapHero(uint playerId, Key hero) => Lobby?.EnqueueAction(() => Lobby?.SwapHero(playerId, hero));

    public void UpdateDeviceSlot(uint playerId, int slot, Key? deviceKey) => Lobby?.EnqueueAction(() => Lobby?.UpdateDeviceSlot(playerId, slot, deviceKey));

    public void SwapDevices(uint playerId, int slot1, int slot2) => Lobby?.EnqueueAction(() => Lobby?.SwapDevices(playerId, slot1, slot2));

    public void ResetToDefaultDevices(uint playerId) => Lobby?.EnqueueAction(() => Lobby?.ResetToDefaultDevices(playerId));

    public void SelectPerk(uint playerId, Key perkKey) => Lobby?.EnqueueAction(() => Lobby?.SelectPerk(playerId, perkKey));

    public void DeselectPerk(uint playerId, Key perkKey) => Lobby?.EnqueueAction(() => Lobby?.DeselectPerk(playerId, perkKey));

    public void SelectSkin(uint playerId, Key skinKey) => Lobby?.EnqueueAction(() => Lobby?.SelectSkin(playerId, skinKey));
    
    public void SelectRole(uint playerId, PlayerRoleType role) => Lobby?.EnqueueAction(() => Lobby?.SelectRole(playerId, role));
    
    public void VoteForMap(uint playerId, Key mapKey) => Lobby?.EnqueueAction(() => Lobby?.VoteForMap(playerId, mapKey));

    public void PlayerReady(uint playerId) => Lobby?.EnqueueAction(() => Lobby?.PlayerReady(playerId));

    public void LoadProgressUpdate(uint playerId, float progress) => Lobby?.EnqueueAction(() => Lobby?.LoadProgressUpdate(playerId, progress));
}