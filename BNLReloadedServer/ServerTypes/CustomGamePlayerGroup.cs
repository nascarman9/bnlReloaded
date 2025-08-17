using System.Diagnostics.CodeAnalysis;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.ServerTypes;

public class CustomGamePlayerGroup(IServiceMatchmaker matchService)
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
    
    public int Spectators { get; private set; }
    
    private Queue<CustomGamePlayer> ChangeTeamRequestsTeam1 { get; } = new();
    private Queue<CustomGamePlayer> ChangeTeamRequestsTeam2 { get; } = new();

    private readonly CustomGameLogic _customLogic = CatalogueHelper.GlobalLogic.CustomGame!;
    
    private TeamType GetBalancedTeam()
    {
        var team1Count = Players.Select(p => p.Team).Count(p => p == TeamType.Team1);
        var team2Count = Players.Select(p => p.Team).Count(p => p == TeamType.Team2);

        return team1Count <= team2Count ? TeamType.Team1 : TeamType.Team2;
    }

    public bool AddPlayer(uint playerId, bool isOwner, ProfileData player)
    {
        if (GameInfo.Players > GameInfo.MaxPlayers)
        {
            return false;
        }

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
        return true;
    }

    public bool KickPlayer(uint playerId, uint kickerId)
    {
        var kicker = Players.FirstOrDefault(p => p.Id == kickerId);
        if (kicker == null) return false;
        if (kicker.Owner && RemovePlayer(playerId))
        {
            matchService.SendCustomGamePlayerKicked(playerId);
            return true;
        }
        return false;
    }

    public bool RemovePlayer(uint playerId)
    {
        var player = Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            return false;
        
        if (!Players.Remove(player))
            return false;
        
        GameInfo.Players--;

        if (Players.Count == 0)
        {
            return Databases.RegionServerDatabase.RemoveCustomGame(GameInfo.Id);
        }
        
        if (player.Owner)
        {
            Players[0].Owner = true;
        }

        switch (player.Team)
        {
            case TeamType.Team1:
                if (ChangeTeamRequestsTeam2.Count > 0)
                {
                    SwapTeam(ChangeTeamRequestsTeam2.Dequeue().Id);
                    return true;
                } 
                break;
            case TeamType.Team2:
                if (ChangeTeamRequestsTeam1.Count > 0)
                {
                    SwapTeam(ChangeTeamRequestsTeam1.Dequeue().Id);
                    return true;
                }
                break;
        }
        
        CustomGameUpdate(players: Players);
        return true;
    }

    public bool SwapTeam(uint playerId)
    {
        var player = Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            return false;

        var swapPlayer = player.Team switch
        {
            TeamType.Team1 => ChangeTeamRequestsTeam2.Count > 0 ? ChangeTeamRequestsTeam2.Dequeue() : null,
            TeamType.Team2 => ChangeTeamRequestsTeam1.Count > 0 ? ChangeTeamRequestsTeam1.Dequeue() : null,
            _ => null
        };

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
            var enemyTeamCount = Players.Select(p => p.Team).Count(p => p != player.Team);
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
                        return false;
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
        return true;
    }

    public bool UpdateSettings(uint playerId, CustomGameSettings settings)
    {
        var player = Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            return false;

        if (!player.Owner) return false;

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
        return true;
    }

    public bool AddSpectator()
    {
        if (Spectators >= _customLogic.MaxSpectatorsPerMatch)
            return false;
        Spectators++;
        return true;
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