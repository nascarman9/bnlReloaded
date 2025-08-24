using System.Collections.Concurrent;
using System.Timers;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.Service;
using Timer = System.Timers.Timer;

namespace BNLReloadedServer.ServerTypes;

public class GameLobby(IServiceLobby serviceLobby) : Updater
{
    private LobbyData LobbyData { get; set; }

    private (LobbyTimerType timerType, Timer timer)? _currentTimer;
    
    private readonly ConcurrentDictionary<Timer, uint> _requeueTimers = new();
    
    private readonly IPlayerDatabase _playerDatabase = Databases.PlayerDatabase;

    private void SetUpTimerEvent()
    {
        if (LobbyData.Timer.TimerType == LobbyTimerType.Requeue) return;
        _currentTimer = (LobbyData.Timer.TimerType, new Timer(LobbyData.Timer.EndTime - LobbyData.Timer.StartTime));
        _currentTimer.Value.timer.AutoReset = false;
        switch(_currentTimer.Value.timerType)
        {
            case LobbyTimerType.Wait:
                _currentTimer.Value.timer.Elapsed += OnWaitTimerElapsed;
                break;
            case LobbyTimerType.Selection:
                _currentTimer.Value.timer.Elapsed += OnSelectionTimerElapsed;
                break;
            case LobbyTimerType.Start:
                _currentTimer.Value.timer.Elapsed += OnStartTimerElapsed;
                break;
            case LobbyTimerType.Requeue:
            default:
                return;
        }
        _currentTimer.Value.timer.Enabled = true;
    }
    
    public void CreateLobbyData(string sessionName, Key matchKey, Key gameModeKey, List<MapInfo> maps)
    {
        LobbyData = new LobbyData();
        var gameCard = Databases.Catalogue.GetCard<CardGameMode>(gameModeKey);
        LobbyTimer timer;
        if (gameCard == null)
        {
            timer = new LobbyTimer
            {
                TimerType = LobbyTimerType.Selection,
                StartTime = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                EndTime = (ulong)DateTimeOffset.Now.AddSeconds(60).ToUnixTimeMilliseconds()
            };
        }
        else if (gameCard.LobbyMode is LobbyModeDraftPick lobbyDraft)
        {
            timer = new LobbyTimer
            {
                TimerType = LobbyTimerType.Wait,
                StartTime = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                EndTime = (ulong)DateTimeOffset.Now.AddSeconds(lobbyDraft.WaitTime).ToUnixTimeMilliseconds()
            };
        }
        else
        {
            var lobbyFree = (LobbyModeFreePick)gameCard.LobbyMode!;
            timer = new LobbyTimer
            {
                TimerType = LobbyTimerType.Selection,
                StartTime = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                EndTime = (ulong)DateTimeOffset.Now.AddSeconds(lobbyFree.SelectionTime).ToUnixTimeMilliseconds()
            };
        }

        var initLobbyUpdate = new LobbyUpdate
        {
            MatchMode = matchKey,
            GameMode = gameModeKey,
            Maps = maps.ConvertAll(info => new LobbyMapData
            {
                Info = info,
                PlayerVotes = []
            }),
            Started = false,
            Timer = timer,
            SessionName = sessionName
        };
        LobbyData.UpdateData(initLobbyUpdate);
        SetUpTimerEvent();
    }

    public void AddPlayer(uint playerId, TeamType team)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var value))
        {
            var profileData = Databases.PlayerDatabase.GetPlayerProfile(playerId);
            var lastPlayedHero = Databases.PlayerDatabase.GetLastPlayedHero(playerId);
            var loadout = Databases.PlayerDatabase.GetLoadoutForHero(playerId, lastPlayedHero);
            var deviceLevels = Databases.PlayerDatabase.GetDeviceLevels(playerId);
            var playerState = new PlayerLobbyState
            {
                PlayerId = playerId,
                SteamId = profileData.SteamId,
                Nickname = profileData.Nickname,
                SquadId = null,
                Role = PlayerRoleType.None,
                PlayerLevel = profileData.Progression?.PlayerProgress?.Level ?? 1,
                SelectedBadges = profileData.SelectedBadges,
                Team = team,
                Hero = lastPlayedHero,
                Devices = loadout.Devices,
                Perks = loadout.Perks,
                RestrictedHeroes = [],
                SkinKey = loadout.SkinKey,
                Ready = LobbyData.Timer.TimerType == LobbyTimerType.Start,
                CanLoadout = true,
                Status = LobbyStatus.Online,
                LookingForFriends = profileData.LookingForFriends,
                DeviceLevels = deviceLevels
            };
            LobbyData.Players.TryAdd(playerId, playerState);
        }
        else
        {
            value.Status = LobbyStatus.Online;
        }
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void PlayerDisconnected(uint playerId)
    {
        if (LobbyData.Players.TryGetValue(playerId, out var value) || value == null) return;
        value.Status = LobbyStatus.Offline;
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void PlayerLeft(uint playerId)
    {
        LobbyData.Players.TryRemove(playerId, out _);
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void SwapHero(uint playerId, Key hero)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var player)) return;
        player.Hero = hero;
        var heroLoadout = _playerDatabase.GetLoadoutForHero(playerId, hero);
        if (heroLoadout.Devices != null)
        { 
            UpdateDevices(player, heroLoadout.Devices); 
        }

        if (heroLoadout.Perks != null)
        {
            UpdatePerks(player, heroLoadout.Perks);
        }
        player.SkinKey = heroLoadout.SkinKey;
        
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    private void UpdateDevices(PlayerLobbyState player, Dictionary<int, Key> devices)
    {
        if (player.Devices == null) return;
        foreach (var slot in devices.Keys)
        {
            player.Devices[slot] = devices[slot];
        }
    }

    public void UpdateDeviceSlot(uint playerId, int slot, Key? deviceKey)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var player) || player.Devices == null) return;
        if (deviceKey != null)
        {
            player.Devices[slot] = deviceKey.Value;
        }
        else
        {
            player.Devices.Remove(slot);
        }
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void SwapDevices(uint playerId, int slot1, int slot2)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var player) || player.Devices == null) return;
        player.Devices.TryGetValue(slot1, out var dev1);
        player.Devices.TryGetValue(slot2, out var dev2);
        if (dev1 == default && dev2 == default) return;
        if (dev1 == default)
        {
            player.Devices.Remove(slot2);
            player.Devices[slot1] = dev2;
        }
        else if (dev2 == default)
        {
            player.Devices.Remove(slot1);
            player.Devices[slot2] = dev1;
        }
        else
        {
            player.Devices[slot1] = dev2;
            player.Devices[slot2] = dev1;
        }
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void ResetToDefaultDevices(uint playerId)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var player)) return;
        player.Devices = CatalogueHelper.GetDefaultDevices(player.Hero);
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    private void UpdatePerks(PlayerLobbyState player, List<Key> perks)
    {
        if (player.Perks == null) return;
        player.Perks = perks;
    }

    public void SelectPerk(uint playerId, Key perkKey)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var player) || player.Perks == null) return;
        var perkCard = Databases.Catalogue.GetCard<CardPerk>(perkKey);
        if (perkCard == null) return;
        var replacePerk = player.Perks.Select(k => Databases.Catalogue.GetCard<CardPerk>(k)).FirstOrDefault(perk => perk?.SlotType == perkCard.SlotType);
        if (replacePerk == null)
        {
            player.Perks.Add(perkKey);
        }
        else
        {
            var perkIdx = player.Perks.IndexOf(replacePerk.Key);
            player.Perks[perkIdx] = perkKey;
        }
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void DeselectPerk(uint playerId, Key perkKey)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var player) || player.Perks == null) return;
        for (var i = 0; i < player.Perks.Count; i++)
        {
            player.Perks.RemoveAll(p => p == perkKey);
        }
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void SelectSkin(uint playerId, Key skinKey)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var player)) return;
        player.SkinKey = skinKey;
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void SelectRole(uint playerId, PlayerRoleType role)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var player)) return;
        player.Role = role;
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void VoteForMap(uint playerId, Key mapKey)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var player)) return;
        var map = LobbyData.Maps.FirstOrDefault(map => (map.Info as MapInfoCard)?.MapKey == mapKey);
        map?.PlayerVotes?.Add(playerId);
        SendLobbyUpdate(maps: LobbyData.Maps);
    }

    public void PlayerReady(uint playerId)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var player)) return;
        player.Ready = true;
        if (LobbyData.Players.Values.All(p => p.Ready = true) && _currentTimer.HasValue)
        {
            OnSelectionTimerElapsed(_currentTimer.Value, new ElapsedEventArgs(DateTime.Now));
        }
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void LoadProgressUpdate(uint playerId, float progress)
    {
        LobbyData.PlayersProgress[playerId] = progress;
        serviceLobby.SendMatchLoadingProgress(LobbyData.PlayersProgress.ToDictionary());
    }

    private void SendLobbyUpdate(Key? matchModeKey = null, Key? gameModeKey = null, List<LobbyMapData>? maps = null,
        bool? started = null, LobbyTimer? timer = null, List<PlayerLobbyState>? players = null,
        Dictionary<TeamType, List<uint>>? requeuePlayers = null, Dictionary<TeamType, LobbyTimer>? requeueTimers = null,
        string? sessionName = null)
    {
        serviceLobby.SendLobbyUpdate(
            new LobbyUpdate
            {
                MatchMode = matchModeKey,
                GameMode = gameModeKey,
                Maps = maps,
                Started = started,
                Timer = timer,
                Players = players,
                RequeuePlayers = requeuePlayers,
                RequeueTimers = requeueTimers,
                SessionName = sessionName
            });
    }

    public LobbyUpdate GetLobbyUpdate()
    {
        return new LobbyUpdate
        {
            MatchMode = LobbyData.MatchModeKey,
            GameMode = LobbyData.GameModeKey,
            Maps = LobbyData.Maps,
            Started = LobbyData.IsStarted,
            Timer = LobbyData.Timer,
            Players = LobbyData.Players.Values.ToList(),
            RequeuePlayers = LobbyData.RequeuePlayers.ToDictionary(),
            RequeueTimers = LobbyData.RequeueTimers.ToDictionary(),
            SessionName = LobbyData.SessionName
        };
    }

    private void OnWaitTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if(_currentTimer == null) return;
        _currentTimer.Value.timer.Stop();
        _currentTimer.Value.timer.Dispose();
        _currentTimer = null;
        
        if (LobbyData.GameMode?.LobbyMode == null) return;
        
        LobbyData.Timer.TimerType = LobbyTimerType.Selection;
        LobbyData.Timer.StartTime = (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (LobbyData.GameMode.LobbyMode is LobbyModeDraftPick gameModeDraft)
        {
            LobbyData.Timer.EndTime = (ulong) DateTimeOffset.Now.AddSeconds(gameModeDraft.SelectionTime).ToUnixTimeMilliseconds();
        }
        else
        {
            var gameModeFree = (LobbyModeFreePick) LobbyData.GameMode.LobbyMode;
            LobbyData.Timer.EndTime = (ulong) DateTimeOffset.Now.AddSeconds(gameModeFree.SelectionTime).ToUnixTimeMilliseconds();
        }
        SendLobbyUpdate(timer: LobbyData.Timer);
        SetUpTimerEvent();
    }

    private void OnSelectionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if(_currentTimer == null) return;
        _currentTimer.Value.timer.Stop();
        _currentTimer.Value.timer.Dispose();
        _currentTimer = null;
        
        if (LobbyData.GameMode?.LobbyMode == null) return;
        
        LobbyData.Timer.TimerType = LobbyTimerType.Start;
        LobbyData.Timer.StartTime = (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds();
        LobbyData.Timer.EndTime = (ulong) DateTimeOffset.Now.AddSeconds(LobbyData.GameMode.LobbyMode.PrestartTime).ToUnixTimeMilliseconds();
        
        EnqueueAction(() =>
            {
                foreach (var pid in LobbyData.Players.Keys)
                {
                    LobbyData.Players[pid].Ready = true;
                }
                SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
            });
        
        SendLobbyUpdate(timer: LobbyData.Timer);
        SetUpTimerEvent();
    }

    private void OnStartTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if(_currentTimer == null) return;
        _currentTimer.Value.timer.Stop();
        _currentTimer.Value.timer.Dispose();
        _currentTimer = null;

        LobbyData.Timer.TimerType = LobbyTimerType.Requeue;
    }

    private void OnRequeueTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        
    }
}