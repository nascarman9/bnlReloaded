using System.Collections.Concurrent;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LobbyData
{
    public bool IsDataExist;
    public Key MatchModeKey;
    public List<LobbyMapData> Maps = [];
    public bool IsStarted;
    public readonly Dictionary<uint, PlayerLobbyState> Players = new();
    public LobbyTimer Timer;
    public Key GameModeKey;
    public readonly Dictionary<TeamType, List<uint>> RequeuePlayers = new();
    public readonly Dictionary<TeamType, LobbyTimer> RequeueTimers = new();
    public readonly Dictionary<uint, float> PlayersProgress = new();

    public string? SessionName { get; private set; }

    public PlayerLobbyState GetState(uint playerId) => Players[playerId];

    public bool IsCustomMode => GameMode?.Ranking == GameRankingType.None;

    public TeamType GetPlayerTeam(uint playerId) => GetPlayer(playerId)?.Team ?? TeamType.Neutral;

    public PlayerLobbyState? GetPlayer(uint playerId) => Players.GetValueOrDefault(playerId);

    public List<PlayerLobbyState> GetTeam(TeamType team) => Players.Values.Where((Func<PlayerLobbyState, bool>) (i => i.Team == team)).ToList();

    public List<PlayerLobbyState> GetTeam1() => GetTeam(TeamType.Team1);

    public List<PlayerLobbyState> GetTeam2() => GetTeam(TeamType.Team2);

    public int GetSquadIndexInTeam(uint playerId)
    {
        var player = GetPlayer(playerId);
        if (player is not { SquadId: not null })
            return -1;
        var ulongList = new List<ulong>();
        foreach (var playerLobbyState in GetTeam(player.Team))
        {
            if (playerLobbyState.SquadId.HasValue && !ulongList.Contains(playerLobbyState.SquadId.Value)) 
                ulongList.Add(playerLobbyState.SquadId.Value);
        }
        return ulongList.IndexOf(player.SquadId.Value);
    }

    public CardMatch? MatchModeCard => IsDataExist ? Databases.Catalogue.GetCard<CardMatch>(MatchModeKey) : null;

    public CardGameMode? GameMode => IsDataExist ? Databases.Catalogue.GetCard<CardGameMode>(GameModeKey) : null;

    public void UpdateData(LobbyUpdate update)
    {
        if (update.MatchMode.HasValue)
            MatchModeKey = update.MatchMode.Value;
        if (update.Maps != null)
            Maps = update.Maps;
        if (update.Started.HasValue)
            IsStarted = update.Started.Value;
        if (update.Players != null)
        {
            foreach (var player in update.Players)
            {
              Players[player.PlayerId] = player;
            }
        }
        if (update.Timer != null)
            Timer = update.Timer;
        if (update.RequeuePlayers != null)
        {
            foreach (var requeuePlayer in update.RequeuePlayers)
                RequeuePlayers[requeuePlayer.Key] = requeuePlayer.Value;
        }
        if (update.RequeueTimers != null)
        {
            foreach (var requeueTimer in update.RequeueTimers)
                RequeueTimers[requeueTimer.Key] = requeueTimer.Value;
        }
        if (update.GameMode.HasValue)
            GameModeKey = update.GameMode.Value;
        if (update.SessionName != null)
            SessionName = update.SessionName;
        IsDataExist = true;
    }

    public void ClearData()
    {
        Players.Clear();
        IsDataExist = false;
        SessionName = null;
    }

    public void UpdatePlayerProgress(Dictionary<uint, float> playersProgress)
    {
        foreach (var player in playersProgress.Keys)
        {
            PlayersProgress[player] = playersProgress[player];
        }
      
    }
}