using System.Collections.Concurrent;
using System.Timers;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.Service;
using Timer = System.Timers.Timer;

namespace BNLReloadedServer.ServerTypes;

public class GameLobby : Updater
{
    private LobbyData LobbyData { get; }

    private (LobbyTimerType timerType, Timer timer)? _currentTimer;
    
    private readonly ConcurrentDictionary<Timer, uint> _requeueTimers = new();
    private readonly ConcurrentDictionary<uint, LobbyTimer> _requeueLobbyTimers = new();
    
    private readonly IPlayerDatabase _playerDatabase = Databases.PlayerDatabase;
    private readonly IServiceLobby _serviceLobby;
    private readonly IGameInstance _gameInstance;

    public GameLobby(IServiceLobby serviceLobby, IGameInstance gameInstance, string sessionName, Key matchKey, Key gameModeKey, List<MapInfo> maps)
    {
        _serviceLobby = serviceLobby;
        _gameInstance = gameInstance;
        LobbyData = new LobbyData();
        var gameCard = Databases.Catalogue.GetCard<CardGameMode>(gameModeKey);
        var timer = gameCard?.LobbyMode switch
        {
            LobbyModeFreePick lobbyFree => new LobbyTimer
            {
                TimerType = LobbyTimerType.Selection,
                StartTime = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                EndTime = (ulong)DateTimeOffset.Now.AddSeconds(lobbyFree.SelectionTime).ToUnixTimeMilliseconds()
            },
            LobbyModeDraftPick lobbyDraft => new LobbyTimer
            {
                TimerType = LobbyTimerType.Wait,
                StartTime = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                EndTime = (ulong)DateTimeOffset.Now.AddSeconds(lobbyDraft.WaitTime).ToUnixTimeMilliseconds()
            },
            _ => new LobbyTimer
            {
                TimerType = LobbyTimerType.Selection,
                StartTime = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                EndTime = (ulong)DateTimeOffset.Now.AddSeconds(60).ToUnixTimeMilliseconds()
            }
        };

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
    
    public void AddPlayer(uint playerId, TeamType team, ulong? squadId)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var value))
        {
            var profileData = Databases.PlayerDatabase.GetPlayerProfile(playerId);
            var lastPlayedHero = Databases.PlayerDatabase.GetLastPlayedHero(playerId);
            var loadout = Databases.PlayerDatabase.GetLoadoutForHero(playerId, lastPlayedHero);
            var deviceLevels = Databases.PlayerDatabase.GetDeviceLevels(playerId);
            var isDraft = LobbyData.GameMode?.LobbyMode is LobbyModeDraftPick;
            var playerState = new PlayerLobbyState
            {
                PlayerId = playerId,
                SteamId = profileData.SteamId,
                Nickname = profileData.Nickname,
                SquadId = squadId,
                Role = PlayerRoleType.None,
                PlayerLevel = profileData.Progression?.PlayerProgress?.Level ?? 1,
                SelectedBadges = profileData.SelectedBadges,
                Team = team,
                Hero = isDraft ? Key.None : lastPlayedHero,
                Devices = isDraft ? new Dictionary<int, Key>() : loadout.Devices,
                Perks = isDraft ? [] : loadout.Perks,
                RestrictedHeroes = [],
                SkinKey = loadout.SkinKey,
                Ready = LobbyData.Timer.TimerType == LobbyTimerType.Start,
                CanLoadout = !isDraft,
                Status = LobbyStatus.Online,
                LookingForFriends = profileData.LookingForFriends,
                DeviceLevels = deviceLevels
            };
            LobbyData.Players.TryAdd(playerId, playerState);
            if (LobbyData.Timer.TimerType == LobbyTimerType.Requeue)
            {
                if (LobbyData.RequeuePlayers.TryGetValue(team, out var requeue))
                {
                    requeue.Add(playerId);
                }
                else
                {
                    LobbyData.RequeuePlayers.Add(team, [playerId]);
                }
                
                if (!_requeueLobbyTimers.ContainsKey(playerId))
                {
                    var timer = GetRequeueTimer(playerId);
                    var requeueTimer = new Timer(timer.EndTime - timer.StartTime);
                    requeueTimer.AutoReset = false;
                    requeueTimer.Elapsed += OnRequeueTimerElapsed;
                    requeueTimer.Enabled = true;
                    _requeueTimers.TryAdd(requeueTimer, playerId);
                    _requeueLobbyTimers.TryAdd(playerId, timer);
                }
                
                SendLobbyUpdate(players: LobbyData.Players.Values.ToList(), requeuePlayers: LobbyData.RequeuePlayers);
                return;
            }
        }
        else
        {
            value.Status = LobbyStatus.Online;
        }
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void PlayerDisconnected(uint playerId)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var value)) return;
        value.Status = LobbyStatus.Offline;
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void PlayerLeft(uint playerId, IServiceLobby? lobbyService)
    {
        if (_gameInstance.IsOver() && LobbyData.Players.TryGetValue(playerId, out var value))
        {
            value.Status = LobbyStatus.Offline;
        }
        else
        {
            LobbyData.Players.Remove(playerId, out _);
        }
        lobbyService?.SendClearLobby();
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

    private static void UpdateDevices(PlayerLobbyState player, Dictionary<int, Key> devices)
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
        if (!LobbyData.Players.TryGetValue(playerId, out var player) || player.Devices == null || slot1 == 1 || slot2 == 1) return;
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

    private static void UpdatePerks(PlayerLobbyState player, List<Key> perks)
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
        if (!LobbyData.Players.ContainsKey(playerId)) return;
        var map = LobbyData.Maps.FirstOrDefault(map => (map.Info as MapInfoCard)?.MapKey == mapKey);
        map?.PlayerVotes?.Add(playerId);
        SendLobbyUpdate(maps: LobbyData.Maps);
    }

    public void StartGame() => SendLobbyUpdate(started: true);

    public void PlayerReady(uint playerId)
    {
        if (!LobbyData.Players.TryGetValue(playerId, out var player)) return;
        player.Ready = true;
        if (LobbyData.Timer.TimerType is LobbyTimerType.Requeue)
        {
            OnRequeueTimerElapsed(_requeueTimers.FirstOrDefault(p => p.Value == playerId).Key,
                new ElapsedEventArgs(DateTime.Now));
        }
        else if (LobbyData.Players.Values.Where(p => p.Hero != Key.None).All(p => p.Ready) && _currentTimer.HasValue)
        {
            OnSelectionTimerElapsed(_currentTimer.Value, new ElapsedEventArgs(DateTime.Now));
        }
        
        SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
    }

    public void LoadProgressUpdate(uint playerId, float progress)
    {
        LobbyData.PlayersProgress[playerId] = progress;
        _serviceLobby.SendMatchLoadingProgress(LobbyData.PlayersProgress.ToDictionary());
    }

    public PlayerLobbyState? GetPlayerLobbyState(uint playerId) => LobbyData.Players.GetValueOrDefault(playerId);

    private LobbyTimer GetRequeueTimer(uint playerId)
    {
        if (_requeueLobbyTimers.TryGetValue(playerId, out var value))
        {
            return value;
        }
        
        return LobbyData.GameMode?.LobbyMode switch
        {
            LobbyModeFreePick lobbyFree => new LobbyTimer
            {
                TimerType = LobbyTimerType.Requeue,
                StartTime = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                EndTime = (ulong)DateTimeOffset.Now.AddSeconds(lobbyFree.ReconnectSelectionTime).ToUnixTimeMilliseconds()
            },
            LobbyModeDraftPick lobbyDraft => new LobbyTimer
            {
                TimerType = LobbyTimerType.Requeue,
                StartTime = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                EndTime = (ulong)DateTimeOffset.Now.AddSeconds(lobbyDraft.ReconnectSelectionTime).ToUnixTimeMilliseconds()
            },
            _ => new LobbyTimer
            {
                TimerType = LobbyTimerType.Requeue,
                StartTime = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                EndTime = (ulong)DateTimeOffset.Now.AddSeconds(60).ToUnixTimeMilliseconds()
            }
        };
    }

    private void SendLobbyUpdate(Key? matchModeKey = null, Key? gameModeKey = null, List<LobbyMapData>? maps = null,
        bool? started = null, LobbyTimer? timer = null, List<PlayerLobbyState>? players = null,
        Dictionary<TeamType, List<uint>>? requeuePlayers = null, Dictionary<TeamType, LobbyTimer>? requeueTimers = null,
        string? sessionName = null)
    {
        _serviceLobby.SendLobbyUpdate(
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

    public LobbyUpdate GetLobbyUpdate(uint playerId) =>
        new()
        {
            MatchMode = LobbyData.MatchModeKey,
            GameMode = LobbyData.GameModeKey,
            Maps = LobbyData.Maps,
            Started = LobbyData.IsStarted,
            Timer = LobbyData.Timer.TimerType == LobbyTimerType.Requeue ? GetRequeueTimer(playerId) : LobbyData.Timer,
            Players = LobbyData.Players.Values.ToList(),
            RequeuePlayers = LobbyData.RequeuePlayers,
            RequeueTimers = LobbyData.RequeueTimers,
            SessionName = LobbyData.SessionName
        };

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
            
            var unselectedPlayers = LobbyData.Players.Where(p => p.Value.Hero == Key.None).Select(p => p.Value).ToList();
            EnqueueAction(() =>
            {
                if (unselectedPlayers.Count > 0)
                {
                    var team1Unselected = unselectedPlayers.Where(p => p.Team is TeamType.Team1).Shuffle().FirstOrDefault();
                    var team2Unselected = unselectedPlayers.Where(p => p.Team is TeamType.Team2).Shuffle().FirstOrDefault();
                    if (team1Unselected != null)
                    {
                        var loadout = Databases.PlayerDatabase.GetLoadoutForHero(team1Unselected.PlayerId,
                            Databases.PlayerDatabase.GetLastPlayedHero(team1Unselected.PlayerId));
                        team1Unselected.Hero = loadout.HeroKey;
                        team1Unselected.Devices = loadout.Devices;
                        team1Unselected.Perks = loadout.Perks;
                        team1Unselected.CanLoadout = true;
                    }

                    if (team2Unselected != null)
                    {
                        var loadout = Databases.PlayerDatabase.GetLoadoutForHero(team2Unselected.PlayerId,
                            Databases.PlayerDatabase.GetLastPlayedHero(team2Unselected.PlayerId));
                        team2Unselected.Hero = loadout.HeroKey;
                        team2Unselected.CanLoadout = true;
                        team2Unselected.Devices = loadout.Devices;
                        team2Unselected.Perks = loadout.Perks;
                    }
                }
                
                SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
            });
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
        
        if (LobbyData.GameMode.LobbyMode is LobbyModeDraftPick gameModeDraft)
        {
            var unselectedPlayers = LobbyData.Players.Where(p => p.Value.Hero == Key.None).Select(p => p.Value).ToList();
            if (unselectedPlayers.Count != 0)
            {
                LobbyData.Timer.TimerType = LobbyTimerType.Selection;
                LobbyData.Timer.StartTime = (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds();
                LobbyData.Timer.EndTime = (ulong) DateTimeOffset.Now.AddSeconds(gameModeDraft.SelectionTime).ToUnixTimeMilliseconds();
            }
            else
            {
                LobbyData.Timer.TimerType = LobbyTimerType.Start;
                LobbyData.Timer.StartTime = (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds();
                LobbyData.Timer.EndTime = (ulong) DateTimeOffset.Now.AddSeconds(gameModeDraft.PrestartTime).ToUnixTimeMilliseconds();
            }
            
            EnqueueAction(() =>
            {
                foreach (var player in LobbyData.Players.Values.Where(p => p.Hero != Key.None))
                {
                    player.Ready = true;
                }

                if (unselectedPlayers.Count > 0)
                {
                    var team1Unselected = unselectedPlayers.Where(p => p.Team is TeamType.Team1).Shuffle().FirstOrDefault();
                    var team2Unselected = unselectedPlayers.Where(p => p.Team is TeamType.Team2).Shuffle().FirstOrDefault();
                    var limit = LobbyData.GameMode.HeroLimit;
                    
                    if (team1Unselected != null)
                    {
                        var loadout = Databases.PlayerDatabase.GetLoadoutForHero(team1Unselected.PlayerId,
                            Databases.PlayerDatabase.GetLastPlayedHero(team1Unselected.PlayerId));
                        
                        List<Key> restricted = [];
                        if (limit?.Limit is not null)
                        {
                            restricted = limit.LimitOption switch
                            {
                                LobbyHeroLimitOption.PerHero => LobbyData.Players.Values
                                    .Where(p => p.Team == TeamType.Team1)
                                    .GroupBy(item => item.Hero)
                                    .Where(group => group.Key != Key.None && group.Count() >= limit.Limit)
                                    .Select(group => group.Key)
                                    .ToList(),
                                LobbyHeroLimitOption.PerClass => LobbyData.Players.Values
                                    .Where(p => p.Team == TeamType.Team1)
                                    .GroupBy(item => (item.Hero.GetCard<CardUnit>()?.Data as UnitDataPlayer)?.Class)
                                    .Where(group => group.Key is not null && group.Key != Key.None && group.Count() >= limit.Limit)
                                    .Select(group => group.Key)
                                    .OfType<Key>()
                                    .SelectMany(key => CatalogueHelper.GetHeroes().Where(p => (p.GetCard<CardUnit>()?.Data as UnitDataPlayer)?.Class == key))
                                    .ToList(),
                                _ => []
                            };
                        }
                        
                        team1Unselected.Hero = loadout.HeroKey;
                        team1Unselected.CanLoadout = true;
                        team1Unselected.Devices = loadout.Devices;
                        team1Unselected.Perks = loadout.Perks;
                        team1Unselected.RestrictedHeroes = restricted;
                    }

                    if (team2Unselected != null)
                    {
                        var loadout = Databases.PlayerDatabase.GetLoadoutForHero(team2Unselected.PlayerId,
                            Databases.PlayerDatabase.GetLastPlayedHero(team2Unselected.PlayerId));
                        
                        List<Key> restricted = [];
                        if (limit?.Limit is not null)
                        {
                            restricted = limit.LimitOption switch
                            {
                                LobbyHeroLimitOption.PerHero => LobbyData.Players.Values
                                    .Where(p => p.Team == TeamType.Team2)
                                    .GroupBy(item => item.Hero)
                                    .Where(group => group.Key != Key.None && group.Count() >= limit.Limit)
                                    .Select(group => group.Key)
                                    .ToList(),
                                LobbyHeroLimitOption.PerClass => LobbyData.Players.Values
                                    .Where(p => p.Team == TeamType.Team2)
                                    .GroupBy(item => (item.Hero.GetCard<CardUnit>()?.Data as UnitDataPlayer)?.Class)
                                    .Where(group => group.Key is not null && group.Key != Key.None && group.Count() >= limit.Limit)
                                    .Select(group => group.Key)
                                    .OfType<Key>()
                                    .SelectMany(key => CatalogueHelper.GetHeroes().Where(p => (p.GetCard<CardUnit>()?.Data as UnitDataPlayer)?.Class == key))
                                    .ToList(),
                                _ => []
                            };
                        }
                        
                        team2Unselected.Hero = loadout.HeroKey;
                        team2Unselected.CanLoadout = true;
                        team2Unselected.Devices = loadout.Devices;
                        team2Unselected.Perks = loadout.Perks;
                        team2Unselected.RestrictedHeroes = restricted;
                    }
                }
                
                SendLobbyUpdate(players: LobbyData.Players.Values.ToList());
            });
        }
        else
        {
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
        }
        
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
        EnqueueAction(() =>
        {
            var mostVoted = LobbyData.Maps.MaxBy(m => m.PlayerVotes?.Count);
            if (mostVoted?.Info is MapInfoCard mapInfo && _gameInstance.IsMapNull() &&
                Databases.MapDatabase.LoadMapData(mapInfo.MapKey) is { } mapData)
            {
                _gameInstance.SetMap(mapInfo, mapData);
            }
            _gameInstance.StartMatch(LobbyData.Players.Values);
        });

    }

    private void OnRequeueTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (sender is not Timer timer) return;
        _requeueTimers.Remove(timer, out var playerId);
        _requeueLobbyTimers.Remove(playerId, out _);
        timer.Stop();
        timer.Dispose();

        EnqueueAction(() =>
        {
            if (LobbyData.Players.TryGetValue(playerId, out var player))
            {
                player.Ready = true;
            }

            if (LobbyData.RequeuePlayers.TryGetValue(LobbyData.GetPlayerTeam(playerId), out var requeuePlayers))
            {
                requeuePlayers.Remove(playerId);
            }
            
            SendLobbyUpdate(players: LobbyData.Players.Values.ToList(), requeuePlayers: LobbyData.RequeuePlayers);
            _gameInstance.SendUserToZone(playerId);
        });
    }
}