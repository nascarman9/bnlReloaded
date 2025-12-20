using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.Servers;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.ServerTypes;

public class SquadData(ulong squadId, Key gameModeKey, ISender sender, IServicePlayer squadUpdater) : Updater
{
    private readonly List<SquadPlayerUpdate> _players = [];
    
    private readonly Dictionary<uint, Guid> _playerGuids = new();
    private readonly Dictionary<uint, IServiceChat> _chatServices = new();
    private readonly Dictionary<uint, IServicePlayer> _playerServices = new();
    
    public required ChatRoom ChatRoom { get; init; }

    private Key GameMode { get; set; } = gameModeKey;

    private readonly IPlayerDatabase _playerDatabase = Databases.PlayerDatabase;

    public void AddPlayer(uint playerId, Guid playerGuid, IServiceChat chatService, IServicePlayer playerService, bool isOwner)
    {
        EnqueueAction(() =>
        {
            var playerData = _playerDatabase.GetPlayerDataNoWait(playerId);
            if (playerData != null && _players.All(p => p.PlayerId != playerId))
            {
                _players.Add(new SquadPlayerUpdate
                {
                    PlayerId = playerId,
                    IsLeader = isOwner,
                    SteamId = playerData.SteamId,
                    Nickname = playerData.Nickname,
                    PlayerLevel = playerData.Progression.PlayerProgress?.Level ?? 0,
                    HeroesLevels = playerData.Progression.HeroesProgress?.Select(k => k.Value.Level).ToList() ?? [],
                    SelectedBadges = playerData.Badges,
                    Graveyard = playerData.GraveyardPermanent ?? false,
                    MmBanEnd = playerData.MatchmakerBanEnd ?? 0
                });

                _playerGuids.TryAdd(playerId, playerGuid);
                sender.Subscribe(playerGuid);
                _chatServices.TryAdd(playerId, chatService);
                _playerServices.TryAdd(playerId, playerService);
                ChatRoom.AddToRoom(playerGuid, chatService);
            }
            
            squadUpdater.SendUpdateSquad(new SquadUpdate
            {
                GameMode = GameMode,
                Players = _players
            });
        });
    }

    public void AddPlayers(List<uint> players, List<Guid> playerGuids, List<IServiceChat> chatServices,
        List<IServicePlayer> playerServices, uint? ownerId)
    {
        EnqueueAction(() =>
        {
            foreach (var (index, playerId) in players.Index())
            {
                var playerData = _playerDatabase.GetPlayerDataNoWait(playerId);
                if (playerData == null || _players.Any(p => p.PlayerId == playerId))
                    continue;

                _players.Add(new SquadPlayerUpdate
                {
                    PlayerId = playerId,
                    IsLeader = ownerId == playerId,
                    SteamId = playerData.SteamId,
                    Nickname = playerData.Nickname,
                    PlayerLevel = playerData.Progression.PlayerProgress?.Level ?? 0,
                    HeroesLevels = playerData.Progression.HeroesProgress?.Select(k => k.Value.Level).ToList() ?? [],
                    SelectedBadges = playerData.Badges,
                    Graveyard = playerData.GraveyardPermanent ?? false,
                    MmBanEnd = playerData.MatchmakerBanEnd ?? 0
                });

                _playerGuids.TryAdd(playerId, playerGuids[index]);
                sender.Subscribe(playerGuids[index]);
                _chatServices.TryAdd(playerId, chatServices[index]);
                _playerServices.TryAdd(playerId, playerServices[index]);
                ChatRoom.AddToRoom(playerGuids[index], chatServices[index]);
            }
            
            squadUpdater.SendUpdateSquad(new SquadUpdate
            {
                GameMode = GameMode,
                Players = _players
            });
        });
    }

    public void RemovePlayer(uint playerId) => EnqueueAction(() => RemovePlayerNoEnqueue(playerId));

    private void RemovePlayerNoEnqueue(uint playerId)
    {
        var player = _players.FirstOrDefault(p => p.PlayerId == playerId);
        var changeOwner = player is { IsLeader: true };
        _players.RemoveAll(p => p.PlayerId == playerId);
        if (_playerGuids.Remove(playerId, out var playerGuid))
        {
            sender.Unsubscribe(playerGuid);
            if (_chatServices.Remove(playerId, out var chatService))
            {
                ChatRoom.RemoveFromRoom(playerGuid, chatService);
            }
            
            if (_playerServices.Remove(playerId, out var playerService))
            {
                playerService.SendUpdateSquad(null);
            }
        }
        
        Databases.RegionServerDatabase.ClearSquadId(playerId);

        if (changeOwner && _players.Count > 0)
        {
            _players[0].IsLeader = true;
        }

        if (_players.Count <= 1)
        {
            CloseSquad();
        }

        squadUpdater.SendUpdateSquad(new SquadUpdate
        {
            GameMode = GameMode,
            Players = _players
        });
    }

    public List<uint> GetPlayers() => _players.Select(p => p.PlayerId).ToList();

    public void ChangeGameMode(Key gameModeKey)
    {
        EnqueueAction(() =>
        {
            GameMode = gameModeKey;
            squadUpdater.SendUpdateSquad(new SquadUpdate
            {
                GameMode = GameMode,
                Players = _players
            });
        });
    }

    private void CloseSquad()
    {
        EnqueueAction(() =>
        {
            foreach (var player in _players.Select(p => p.PlayerId).ToList())
            {
                RemovePlayerNoEnqueue(player);
            }

            ChatRoom.ClearRoom();
            Databases.RegionServerDatabase.CloseSquad(squadId);
        });
    }

    public bool IsOwner(uint playerId)
    {
        var player = _players.FirstOrDefault(p => p.PlayerId == playerId);
        return player?.IsLeader ?? false;
    }
}