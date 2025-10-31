using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.ServerTypes;

public class CustomGamePlayerGroup(IServiceMatchmaker matchService) : Updater, IGameInitiator
{
    private readonly CustomGameInfo _gameInfo;
    public required string Password { get; init; }
    public required CustomGameInfo GameInfo
    {
        get => _gameInfo;

        [MemberNotNull(nameof(_gameInfo))]
        init
        {
           _gameInfo = value; 
           CustomGameUpdate(gameName: value.GameName, pass: Password, mapInfo: value.MapInfo, buildTime: value.BuildTime,
               respawnMod: value.RespawnTimeMod, heroSwitch: value.HeroSwitch, superSupply: value.SuperSupply,
               allowBackfilling: value.AllowBackfilling, resourceCap: value.ResourceCap, initResources: value.InitResource,
               players: Players, status: value.Status);
        } 
    }

    public List<CustomGamePlayer> Players { get; } = [];
    public List<uint> Spectators { get; } = [];
    
    public string? GameInstanceId { get; set; }

    public required ChatRoom ChatRoom { get; init; }

    private ConcurrentQueue<CustomGamePlayer> ChangeTeamRequestsTeam1 { get; } = new();
    private ConcurrentQueue<CustomGamePlayer> ChangeTeamRequestsTeam2 { get; } = new();

    private readonly CustomGameLogic _customLogic = CatalogueHelper.GlobalLogic.CustomGame!;

    private TeamType GetBalancedTeam()
    {
        var playerArray = Players.ToArray();
        var team1Count = playerArray.Select(p => p.Team).Count(p => p == TeamType.Team1);
        var team2Count = playerArray.Select(p => p.Team).Count(p => p == TeamType.Team2);

        return team1Count <= team2Count ? TeamType.Team1 : TeamType.Team2;
    }

    public bool AddPlayer(uint playerId, bool isOwner, ProfileData player)
    {
        if (GameInfo.Players > GameInfo.MaxPlayers)
        {
            return false;
        }
        EnqueueAction(() =>
        {
            Players.Add(new CustomGamePlayer 
                {
                    Id = playerId,
                    SteamId = player.SteamId,
                    Nickname = player.Nickname,
                    PlayerLevel = player.Progression!.PlayerProgress.Level,
                    SelectedBadges = player.SelectedBadges,
                    Owner = isOwner,
                    Team = GetBalancedTeam(),
                    SwitchTeamRequest = false
                });
            GameInfo.Players++;
                    
            CustomGameUpdate(players: Players);
        });
        return true;
    }

    public bool KickPlayer(uint playerId, uint kickerId)
    {
        var kicker = Players.FirstOrDefault(p => p.Id == kickerId);
        if (kicker == null) return false;
        if (!kicker.Owner) return false;
        RemovePlayer(playerId);
        EnqueueAction(() =>
        {
            matchService.SendCustomGamePlayerKicked(playerId);
        });
        return true;
    }

    public bool RemovePlayer(uint playerId)
    {
        var player = Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            return false;

        EnqueueAction(() =>
        {
            Players.Remove(player);
            GameInfo.Players--;
            
            if (Players.Count <= 0)
            {
                CloseCustomGame();
                return;
            }
            
            if (player.Owner)
            {
                Players[0].Owner = true;
            }

            switch (player.Team)
            {
                case TeamType.Team1:
                    if (!ChangeTeamRequestsTeam2.IsEmpty)
                    {
                        if (ChangeTeamRequestsTeam2.TryDequeue(out var swapPlayer))
                        {
                            SwapTeam(swapPlayer.Id);
                            return;
                        }
                            
                    } 
                    break;
                case TeamType.Team2:
                    if (!ChangeTeamRequestsTeam1.IsEmpty)
                    {
                        if (ChangeTeamRequestsTeam1.TryDequeue(out var swapPlayer))
                        {
                            SwapTeam(swapPlayer.Id);
                            return;
                        }
                    }
                    break;
            }
            
            CustomGameUpdate(players: Players);
        });
        return true;
    }

    public void CloseCustomGame()
    {
        Databases.RegionServerDatabase.RemoveCustomGame(GameInfo.Id);
        ChatRoom.ClearRoom();
    }

    public void SwapTeam(uint playerId)
    {
        var playerArray = Players.ToArray();
        var player = playerArray.FirstOrDefault(p => p.Id == playerId);

        if (player == null)
            return;

        CustomGamePlayer? swapPlayer = null;
        switch (player.Team)
        {
            case TeamType.Team1:
                if(!ChangeTeamRequestsTeam2.IsEmpty) ChangeTeamRequestsTeam2.TryDequeue(out swapPlayer);
                break;
            case TeamType.Team2:
                if(!ChangeTeamRequestsTeam1.IsEmpty) ChangeTeamRequestsTeam1.TryDequeue(out swapPlayer);
                break;
            case TeamType.Neutral:
            default:
                swapPlayer = null;
                break;
        }

        if (swapPlayer != null)
        {
            var myTeam = player.Team;
            var otherTeam = swapPlayer.Team;
            player.Team = otherTeam;
            swapPlayer.Team = myTeam;
            player.SwitchTeamRequest = false;
            swapPlayer.SwitchTeamRequest = false;
        }
        else
        {
            var enemyTeamCount = playerArray.Select(p => p.Team).Count(p => p != player.Team);
            if (enemyTeamCount >= GameInfo.MaxPlayers / 2)
            {
                player.SwitchTeamRequest = true;
                switch (player.Team)
                {
                    case TeamType.Neutral:
                        break;
                    case TeamType.Team1:
                        ChangeTeamRequestsTeam1.Enqueue(player);
                        break;
                    case TeamType.Team2:
                        ChangeTeamRequestsTeam2.Enqueue(player);
                        break;
                }
            }
            else
            {
                switch (player.Team)
                {
                    case TeamType.Neutral:
                        return;
                    case TeamType.Team1:
                        player.Team = TeamType.Team2;
                        player.SwitchTeamRequest = false;
                        break;
                    case TeamType.Team2:
                        player.Team = TeamType.Team1;
                        player.SwitchTeamRequest = false;
                        break;
                }
            }
        }
        CustomGameUpdate(players: Players);
    }

    public void UpdateSettings(uint playerId, CustomGameSettings settings)
    {
        var player = Players.ToArray().FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            return;

        if (!player.Owner) return;

        if (settings.BuildTime.HasValue)
            settings.BuildTime = float.Clamp(settings.BuildTime.Value, _customLogic.MinBuildTime, _customLogic.MaxBuildTime);
        if (settings.RespawnTimeMod.HasValue)
            settings.RespawnTimeMod = float.Clamp(settings.RespawnTimeMod.Value, _customLogic.MinRespawnTimeMod, _customLogic.MaxRespawnTimeMod);
        if (settings.ResourceCap.HasValue)
            settings.ResourceCap = float.Clamp(settings.ResourceCap.Value, _customLogic.MinResourceCap, _customLogic.MaxResourceCap);
        if (settings.InitResource.HasValue)
            settings.InitResource = float.Clamp(settings.InitResource.Value, _customLogic.MinInitResource, _customLogic.MaxInitResource);
        
        GameInfo.MapInfo = settings.MapInfo ?? GameInfo.MapInfo;
        GameInfo.BuildTime = settings.BuildTime ?? GameInfo.BuildTime;
        GameInfo.RespawnTimeMod = settings.RespawnTimeMod ?? GameInfo.RespawnTimeMod;
        GameInfo.HeroSwitch = settings.HeroSwitch ?? GameInfo.HeroSwitch;
        GameInfo.SuperSupply = settings.SuperSupply ?? GameInfo.SuperSupply;
        GameInfo.AllowBackfilling = settings.AllowBackfilling ?? GameInfo.AllowBackfilling;
        GameInfo.ResourceCap = settings.ResourceCap ?? GameInfo.ResourceCap;
        GameInfo.InitResource = settings.InitResource ?? GameInfo.InitResource;

        if (GameInfo.InitResource > GameInfo.ResourceCap)
        {
            GameInfo.InitResource = GameInfo.ResourceCap;
            settings.InitResource = GameInfo.InitResource;
        }
        
        CustomGameUpdate(mapInfo: settings.MapInfo, buildTime: settings.BuildTime, respawnMod: settings.RespawnTimeMod,
            heroSwitch: settings.HeroSwitch, superSupply: settings.SuperSupply, allowBackfilling: settings.AllowBackfilling, 
            resourceCap: settings.ResourceCap, initResources: settings.InitResource);
    }

    public void AddSpectator(uint playerId)
    {
        if (Spectators.Count >= _customLogic.MaxSpectatorsPerMatch)
            return;
        Spectators.Add(playerId);
    }

    public bool StartIntoLobby(uint playerId)
    {
        var player = Players.ToArray().FirstOrDefault(p => p.Id == playerId);
        if (player == null) return false;
        if (!player.Owner) return false;
        GameInfo.Status = CustomGameStatus.Lobby;
        
        CustomGameUpdate(status: GameInfo.Status);
        return true;
    }
    
    public void StartIntoMatch()
    {
        GameInfo.Status = CustomGameStatus.Match;
        CustomGameUpdate(status: GameInfo.Status);
    }

    public void ClearInstance()
    {
        GameInstanceId = null;
        GameInfo.Status = CustomGameStatus.Preparing;
        CustomGameUpdate(status: GameInfo.Status);
    }

    public TeamType GetTeamForPlayer(uint playerId)
    {
        var playerArray = Players.ToArray();
        var player = playerArray.FirstOrDefault(p => p.Id == playerId);
        return player?.Team ?? TeamType.Team1; 
    }

    public bool IsPlayerSpectator(uint playerId)
    {
        return Spectators.Contains(playerId);
    }

    public Key GetGameMode()
    {
        return CatalogueHelper.ModeCustom.Key;
    }

    public bool CanSwitchHero()
    {
        return _gameInfo.HeroSwitch;
    }

    public bool IsMapEditor()
    {
        return false;
    }

    public float GetResourceCap()
    {
        return _gameInfo.ResourceCap;
    }

    public float GetResourceAmount()
    {
        return _gameInfo.InitResource;
    }

    public long? GetBuildPhaseEndTime(DateTimeOffset startTime)
    {
        return startTime.AddSeconds((long) _gameInfo.BuildTime).ToUnixTimeMilliseconds();
    }

    public float GetRespawnMultiplier()
    {
        return _gameInfo.RespawnTimeMod;
    }

    public CustomGameUpdate GetCustomGameUpdate()
    {
        var settings = new CustomGameSettings
        {
            MapInfo = GameInfo.MapInfo,
            BuildTime = GameInfo.BuildTime,
            RespawnTimeMod = GameInfo.RespawnTimeMod,
            HeroSwitch = GameInfo.HeroSwitch,
            SuperSupply = GameInfo.SuperSupply,
            AllowBackfilling = GameInfo.AllowBackfilling,
            ResourceCap = GameInfo.ResourceCap,
            InitResource = GameInfo.InitResource
        };
        
        return new CustomGameUpdate
        {
            GameName = GameInfo.GameName,
            Password = Password,
            Settings = settings,
            Players = Players,
            Status = GameInfo.Status
        };
    }

    private void CustomGameUpdate(string? gameName = null, string? pass = null,
        MapInfo? mapInfo = null, float? buildTime = null, float? respawnMod = null, bool? heroSwitch = null, 
        bool? superSupply = null, bool? allowBackfilling = null, float? resourceCap = null, float? initResources = null,
        List<CustomGamePlayer>? players = null, CustomGameStatus? status = null)
    {
        CustomGameSettings? settings = null;
        if (mapInfo != null || buildTime != null || respawnMod != null || heroSwitch != null || superSupply != null ||
            allowBackfilling != null || resourceCap != null || initResources != null)
            settings = new CustomGameSettings
            {
                MapInfo = mapInfo,
                BuildTime = buildTime,
                RespawnTimeMod = respawnMod,
                HeroSwitch = heroSwitch,
                SuperSupply = superSupply,
                AllowBackfilling = allowBackfilling,
                ResourceCap = resourceCap,
                InitResource = initResources
            };

        matchService.SendUpdateCustomGame(
            new CustomGameUpdate
            {
                GameName = gameName,
                Password = pass,
                Settings = settings,
                Players = players,
                Status = status
            });
    }

    
}