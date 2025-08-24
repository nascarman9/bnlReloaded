using System.Collections.Concurrent;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Servers;
using BNLReloadedServer.ServerTypes;
using BNLReloadedServer.Service;
using NetCoreServer;

namespace BNLReloadedServer.Database;

public class GameInstance(TcpServer server) : IGameInstance
{
    private class MatchConnectionInfo(Guid guid)
    {
        public Guid Guid { get; } = guid;
    }
    
    public required string GameInstanceId { get; init; }

    public required IGameInitiator GameInitiator { get; init; }

    private MapData? MapData { get; set; }

    private GameLobby? Lobby { get; set; }
    
    private readonly ConcurrentDictionary<uint, MatchConnectionInfo> _connectedUsers = new();

    private readonly ConcurrentDictionary<Guid, TcpSession> _lobbySessions = new();
    private readonly ConcurrentDictionary<Guid, TcpSession> _zoneSessions = new();
    
    private readonly Dictionary<Guid, Dictionary<ServiceId, IService>> _services = new();

    public void LinkGuidToPlayer(uint userId, Guid guid)
    {
        _connectedUsers[userId] = new MatchConnectionInfo(guid);
    }
    
    public void UserEnteredLobby(uint userId)
    {
        if (!_connectedUsers.TryGetValue(userId, out var value) || Lobby == null) return;
        Lobby.EnqueueAction(() => Lobby.AddPlayer(userId, GameInitiator.GetTeamForPlayer(userId)));
        var playerGuid = value.Guid;
        _lobbySessions[playerGuid] = server.FindSession(playerGuid);
        _services.TryGetValue(playerGuid, out var service);
        if (service?[ServiceId.ServiceLobby] is ServiceLobby lobbyService)
        {
            lobbyService.SendLobbyUpdate(Lobby.GetLobbyUpdate());
        }
    }

    public void PlayerDisconnected(uint userId)
    {
        Lobby?.EnqueueAction(() => Lobby?.PlayerDisconnected(userId));
    }
    
    public void PlayerLeftInstance(uint userId)
    {
        _connectedUsers.TryRemove(userId, out _);
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
        _lobbySessions.TryRemove(sessionId, out _);
        _zoneSessions.TryRemove(sessionId, out _);
    }

    public void CreateLobby(Key gameModeKey, MapInfo? mapInfo)
    {
        var gameMode = Databases.Catalogue.GetCard<CardGameMode>(gameModeKey);
        if (gameMode == null) return;
        
        Lobby = new GameLobby(new ServiceLobby(new SessionSender(_lobbySessions)));
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