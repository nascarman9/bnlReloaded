using System.Numerics;
using System.Timers;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.Servers;
using BNLReloadedServer.Service;
using MatchType = BNLReloadedServer.BaseTypes.MatchType;
using Timer = System.Timers.Timer;

namespace BNLReloadedServer.ServerTypes;

public class GameZone : Updater
{
    public CancellationTokenSource GameCanceler { get; } = new();
    
    private readonly ZoneData _zoneData;
    public ZoneInitData BeginningZoneInitData { get; }
    
    private readonly IServiceZone _serviceZone;
    private readonly IServiceZone _unbufferedZone;
    private readonly IBuffer _sendBuffer;
    private readonly ISender _sessionsSender;
    
    private readonly IGameInitiator _gameInitiator;
    
    private readonly List<PlayerLobbyState> _playerLobbyInfo;
    private readonly Dictionary<uint, Unit> _units = new();
    private readonly Dictionary<uint, Unit> _playerUnits = new();
    private readonly Dictionary<uint, uint> _playerIdToUnitId = new();
    private readonly Dictionary<uint, MapSpawnPoint> _mapSpawnPoints = new();
    private readonly uint[] _defaultSpawnId = new uint[Enum.GetValues<TeamType>().Length];
    private readonly Queue<UnitLabel>[] _objectiveConquest = new Queue<UnitLabel>[Enum.GetValues<TeamType>().Length];

    private Task? _gameLoop;

    private Timer? _build1Timer;
    private Timer? _build2Timer;
    
    private uint _newUnitId = 1;
    private uint _newSpawnId = 1;

    private uint NewUnitId => _newUnitId++;
    private uint NewSpawnId => _newSpawnId++;

    public GameZone(IServiceZone serviceZone, IServiceZone unbufferedZone, IBuffer sendBuffer, ISender sessionsSender, MapData mapData, IGameInitiator gameInitiator, List<PlayerLobbyState> players, Key? mapKey = null)
    {
        _serviceZone = serviceZone;
        _unbufferedZone = unbufferedZone;
        _sendBuffer = sendBuffer;
        _sessionsSender = sessionsSender;
        _gameInitiator = gameInitiator;
        _playerLobbyInfo = players;
        var spawns = new Dictionary<uint, SpawnPoint>();

        foreach (var spawnPoint in mapData.SpawnPoints)
        {
            var spawnId = NewSpawnId;
            spawns.Add(spawnId, new SpawnPoint
            {
                Id = spawnId,
                Team = spawnPoint.Team,
                Pos = spawnPoint.Position,
                Lock = SpawnPointLockType.Free,
            });
            _mapSpawnPoints.Add(spawnId, spawnPoint);

            if (_defaultSpawnId[(int)spawnPoint.Team] == 0 && spawnPoint.Label == SpawnPointLabel.Base)
            {
                _defaultSpawnId[(int)spawnPoint.Team] = spawnId;
            }
        }

        var playerMap = _playerLobbyInfo.ToDictionary(player => player.PlayerId,
            player => new ZonePlayerInfo
            {
                Nickname = player.Nickname, 
                SteamId = player.SteamId, 
                SquadId = player.SquadId,
                LookingForFriends = player.LookingForFriends
            });

        var match = CatalogueHelper.GetMatch(mapData.Match);
        var startingPhase = match.Data.Type is MatchType.ShieldCapture or MatchType.ShieldRush2
            ? ZonePhaseType.Waiting
            : ZonePhaseType.TutorialInit;

        _zoneData = new ZoneData
        {
            MatchKey = match.Key,
            GameModeKey = gameInitiator.GetGameMode(),
            MapData = mapData,
            MapKey = mapKey,
            CanSwitchHero = gameInitiator.CanSwitchHero(),
            Phase = new ZonePhase
            {
                PhaseType = startingPhase,
                StartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            }
        };
        
        BeginningZoneInitData = _zoneData.GetZoneInitData();
        EnqueueAction(() =>
        {
            _zoneData.SpawnPoints = spawns;
            _zoneData.PlayerInfo = playerMap;
            _zoneData.ResourceCap = gameInitiator.GetResourceCap();
            CreateMapUnits();
        });
    }

    public void SendInitializeZone(IServiceZone zoneService)
    {
        zoneService.SendInitZone(BeginningZoneInitData);
    }

    public void SendLoadZone(IServiceZone zoneService, uint playerId)
    {
        zoneService.SendUpdateZone(GetInitialZoneUpdate());
        zoneService.SendUpdateBarriers(GetBarriersForPhase(_zoneData.Phase.PhaseType));
        foreach (var unit in _units)
        {
            if (unit.Value.OwnerPlayerId == playerId)
            {
                var init = unit.Value.GetInitData();
                init.Controlled = true;
                zoneService.SendUnitCreate(unit.Key, init);
            }
            else
            {
                zoneService.SendUnitCreate(unit.Key, unit.Value.GetInitData());
            }
            zoneService.SendUnitUpdate(unit.Key, unit.Value.GetUpdateData());
        }

        foreach (var player in _playerUnits)
        {
            if (player.Value.PlayerId == playerId)
            {
                var init = player.Value.GetInitData();
                init.Controlled = true;
                zoneService.SendUnitCreate(player.Key, init);
            }
            else
            {
                zoneService.SendUnitCreate(player.Key, player.Value.GetInitData());
            }
            zoneService.SendUnitUpdate(player.Key, player.Value.GetUpdateData());
        }

        if (_playerUnits.Values.Any(player => player.PlayerId == playerId)) return;
        var playerUnit = CreatePlayerUnit(playerId);
        if (playerUnit == null) return;
        _playerUnits.Add(playerUnit.Id, playerUnit);
        _playerIdToUnitId.Add(playerId, playerUnit.Id);
        PlayerUnitCreated(playerUnit);
        var unitInit = playerUnit.GetInitData();
        unitInit.Controlled = true;
        zoneService.SendUnitCreate(playerUnit.Id, unitInit);
        zoneService.SendUnitUpdate(playerUnit.Id, playerUnit.GetUpdateData());
    }

    private Unit? CreatePlayerUnit(uint playerId)
    {
        var playerInfo = _playerLobbyInfo.Find(player => player.PlayerId == playerId);
        if (playerInfo == null) return null;
        var spawnId = _defaultSpawnId[(int)playerInfo.Team];
        var spawnPoint = _mapSpawnPoints.GetValueOrDefault(spawnId);
        var pos = Vector3.Zero;
        var rot = Vector3s.Zero;
        if (spawnPoint != null)
        {
            pos = spawnPoint.Position;
            rot = spawnPoint.Direction switch
            {
                Direction2D.Left => Vector3s.Left,
                Direction2D.Right => Vector3s.Right,
                Direction2D.Back => Vector3s.Back,
                Direction2D.Front => Vector3s.Forward,
                _ => Vector3s.Zero
            };
        }

        var transform = new ZoneTransform
        {
            Position = pos,
            Rotation = rot,
            LocalVelocity = Vector3s.Zero
        };
        
        var unitId = NewUnitId;
        
        return CatalogueFactory.CreatePlayerUnit(unitId, playerInfo.PlayerId, transform, playerInfo, _gameInitiator);
    }
    
    private void PlayerUnitCreated(Unit playerUnit)
    {
        _unbufferedZone.SendUnitCreate(playerUnit.Id, playerUnit.GetInitData());
        _unbufferedZone.SendUnitUpdate(playerUnit.Id, playerUnit.GetUpdateData());
    }
    
    // Map units are controlled by everyone in the match
    private void CreateMapUnits()
    {
        foreach (var unit in _zoneData.MapData.Units)
        {
            var unitId = NewUnitId;
            var newUnit = CatalogueFactory.CreateUnit(unitId, unit);
            if (newUnit != null) 
                _units.Add(unitId, newUnit);
        }
        SetUpObjectives();
    }

    private void SetUpObjectives()
    {
        if (_zoneData.MatchCard.Data?.Type == MatchType.TimeTrial) return;

        foreach (var team in Enum.GetValues<TeamType>())
        {
            _objectiveConquest[(int)team] = new Queue<UnitLabel>();
        }

        foreach (var objLabel in (UnitLabel[])[UnitLabel.Line1, UnitLabel.Line2, UnitLabel.Line3, UnitLabel.LineBase])
        {
            foreach (var team in Enum.GetValues<TeamType>())
            {
                if (_units.Values.Any(unit => (unit.UnitCard.Labels?.Contains(objLabel) ?? false) && unit.Team == team))
                {
                    _objectiveConquest[(int) team].Enqueue(objLabel);                                        
                }
            }
        }

        var teamFirst = new UnitLabel[Enum.GetValues<TeamType>().Length];
        foreach (var team in Enum.GetValues<TeamType>())
        {
            teamFirst[(int) team] = _objectiveConquest[(int) team].Count > 0 ? _objectiveConquest[(int) team].Peek() : UnitLabel.Objective;
        }

        var effects = CatalogueHelper.GetCards<CardEffect>(CardCategory.Effect);
        var lineEffects = new Dictionary<UnitLabel, Dictionary<BuffType, float>>();

        foreach (var objLabel in (UnitLabel[])[UnitLabel.Line1, UnitLabel.Line2, UnitLabel.Line3, UnitLabel.LineBase])
        {
            lineEffects.Add(objLabel, new Dictionary<BuffType, float>());
            effects.Where(effect =>
                    effect.Effect?.Targeting?.AffectedLabels != null &&
                    effect.Effect.Targeting.AffectedLabels.Contains(objLabel))
                .Select(effect => effect.Effect as ConstEffectBuff)
                .OfType<ConstEffectBuff>()
                .Select(buff => buff.Buffs)
                .OfType<Dictionary<BuffType, float>>()
                .ToList()
                .ForEach(x =>
                    x.ToList().ForEach(kv =>
                    {
                        if (lineEffects[objLabel].TryGetValue(kv.Key, out var lastVal))
                        {
                            if (kv.Value > lastVal)
                            {
                                lineEffects[objLabel][kv.Key] = kv.Value;
                            }
                        }
                        else
                        {
                            lineEffects[objLabel][kv.Key] = kv.Value;
                        }
                    })
                );
        }

        foreach (var unit in _units.Values)
        {
            if (unit.UnitCard.Labels == null || !unit.UnitCard.Labels.Contains(UnitLabel.Objective) ||
                unit.UnitCard.Labels.Contains(teamFirst[(int)unit.Team])) continue;
            if (unit.UnitCard.Labels.Contains(UnitLabel.Line1))
            {
                foreach (var buff in lineEffects[UnitLabel.Line1])
                {
                    unit.Buffs.Add(buff.Key, buff.Value);
                }
            }
            else if (unit.UnitCard.Labels.Contains(UnitLabel.Line2))
            {
                foreach (var buff in lineEffects[UnitLabel.Line2])
                {
                    unit.Buffs.Add(buff.Key, buff.Value);
                }
            }
            else if (unit.UnitCard.Labels.Contains(UnitLabel.Line3))
            {
                foreach (var buff in lineEffects[UnitLabel.Line3])
                {
                    unit.Buffs.Add(buff.Key, buff.Value);
                }
            }
            else if (unit.UnitCard.Labels.Contains(UnitLabel.LineBase))
            {
                foreach (var buff in lineEffects[UnitLabel.LineBase])
                {
                    unit.Buffs.Add(buff.Key, buff.Value);
                }
            }
        }
    }

    private ZoneUpdate GetInitialZoneUpdate()
    {
        return new ZoneUpdate
        {
            Phase = _zoneData.Phase,
            PlayerInfo = _zoneData.PlayerInfo,
            SpawnPoints = _zoneData.SpawnPoints.Values.ToList(),
            ResourceCap = _zoneData.ResourceCap,
        };
    }

    public void BeginBuildPhase()
    {
        if (_zoneData.Phase.PhaseType is not (ZonePhaseType.Waiting or ZonePhaseType.TutorialInit)) return;
        UpdatePhase();
    }

    private void UpdatePhase()
    {
        var currentPhase = _zoneData.Phase.PhaseType;
        ZonePhaseType nextPhase;
        var startTime = DateTimeOffset.Now;
        long? endTime = null;

        switch (currentPhase)
        {
            case ZonePhaseType.Waiting: 
            case ZonePhaseType.TutorialInit:
            {
                nextPhase = ZonePhaseType.Build;
                endTime = _gameInitiator.GetBuildPhaseEndTime(startTime) ?? _zoneData.MatchCard.Data switch
                {
                    null => null,
                    MatchDataShieldCapture matchDataShieldCapture => startTime
                        .AddSeconds(matchDataShieldCapture.Build1Time).ToUnixTimeMilliseconds(),
                    MatchDataShieldRush2 matchDataShieldRush2 => startTime
                        .AddSeconds(matchDataShieldRush2.Build1Time).ToUnixTimeMilliseconds(),
                    MatchDataTimeTrial matchDataTimeTrial => startTime
                        .AddSeconds(matchDataTimeTrial.PrestartTime).ToUnixTimeMilliseconds(),
                    MatchDataTutorial matchDataTutorial => startTime.AddSeconds(matchDataTutorial.BuildTime)
                        .ToUnixTimeMilliseconds(),
                    _ => null
                };
                
                if (endTime != null)
                {
                    _build1Timer = new Timer(TimeSpan.FromMilliseconds(endTime.Value - startTime.ToUnixTimeMilliseconds()));
                    _build1Timer.AutoReset = false;
                    _build1Timer.Elapsed += OnBuild1TimerElapsed;
                    _build1Timer.Start();
                }
                break;
            }
            case ZonePhaseType.Build:
                nextPhase = ZonePhaseType.Assault;
                break;
            case ZonePhaseType.Assault:
                nextPhase = ZonePhaseType.Build2;
                
                endTime = _gameInitiator.GetBuildPhaseEndTime(startTime) ?? _zoneData.MatchCard.Data switch
                {
                    null => null,
                    MatchDataShieldCapture matchDataShieldCapture => startTime
                        .AddSeconds(matchDataShieldCapture.Build1Time).ToUnixTimeMilliseconds(),
                    MatchDataShieldRush2 matchDataShieldRush2 => startTime
                        .AddSeconds(matchDataShieldRush2.Build1Time).ToUnixTimeMilliseconds(),
                    MatchDataTimeTrial matchDataTimeTrial => startTime
                        .AddSeconds(matchDataTimeTrial.PrestartTime).ToUnixTimeMilliseconds(),
                    MatchDataTutorial matchDataTutorial => startTime.AddSeconds(matchDataTutorial.BuildTime)
                        .ToUnixTimeMilliseconds(),
                    _ => null
                };

                if (endTime != null)
                {
                    _build2Timer = new Timer(TimeSpan.FromMilliseconds(endTime.Value - startTime.ToUnixTimeMilliseconds()));
                    _build2Timer.AutoReset = false;
                    _build2Timer.Elapsed += OnBuild2TimerElapsed;
                    _build2Timer.Start();
                }
                break;
            case ZonePhaseType.Build2:
                nextPhase = ZonePhaseType.Assault2;
                break;
            case ZonePhaseType.Assault2:
            case ZonePhaseType.SuddenDeath:
            default:
                nextPhase = ZonePhaseType.SuddenDeath;
                break;
        }

        _zoneData.Phase = new ZonePhase
        {
            PhaseType = nextPhase,
            StartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            EndTime = endTime,
        };
        
        _serviceZone.SendUpdateZone(new ZoneUpdate
        {
            Phase = _zoneData.Phase
        });
        _serviceZone.SendUpdateBarriers(GetBarriersForPhase(nextPhase));

        if (currentPhase is not (ZonePhaseType.Waiting or ZonePhaseType.TutorialInit)) return;
        var initMatchStats = new MatchStats
        {
            PlayerStats = new Dictionary<uint, MatchPlayerStats>(),
            Team1Stats = new MatchTeamStats
            {
                Warfare = 0,
                Construction = 0,
                Tactics = 0,
                Healing = 0
            },
            Team2Stats = new MatchTeamStats
            {
                Warfare = 0,
                Construction = 0,
                Tactics = 0,
                Healing = 0
            }
        };
        
        var spawnPoints = new Dictionary<uint, uint?>();

        foreach (var player in _playerLobbyInfo)
        {
            initMatchStats.PlayerStats.Add(player.PlayerId, new MatchPlayerStats
            {
                Team = player.Team,
                Kills = 0,
                Deaths = 0,
                Assists = 0
            });
            
            _mapSpawnPoints.TryGetValue(_defaultSpawnId[(int) player.Team], out var spawn);
            
            spawnPoints.Add(player.PlayerId, spawn != null ? _defaultSpawnId[(int) player.Team] : null);
        }

        var matchZoneUpdate = new ZoneUpdate
        {
            Statistics = initMatchStats,
            PlayerSpawnPoints = spawnPoints,
        };

        _zoneData.UpdateData(matchZoneUpdate);
        _serviceZone.SendUpdateZone(matchZoneUpdate);

        foreach (var unit in _playerUnits.Values)
        {
            PlayerMovementActive(unit);
        }

        _gameLoop = RunGameLoop();
    }

    public void JoinedInProgress(uint playerId)
    {
        var lobbyInfo = _playerLobbyInfo.FirstOrDefault(x => x.PlayerId == playerId);
        if (lobbyInfo == null) return;
        if (!_zoneData.PlayerStats.ContainsKey(playerId))
        {
            _zoneData.PlayerStats.Add(playerId, new MatchPlayerStats
            {
                Team = lobbyInfo.Team,
                Kills = 0,
                Deaths = 0,
                Assists = 0
            });
        }
        
        _mapSpawnPoints.TryGetValue(_defaultSpawnId[(int) lobbyInfo.Team], out var spawn);
        _zoneData.PlayerSpawnPoints[playerId] = spawn != null ? _defaultSpawnId[(int) lobbyInfo.Team] : null;
        
        var matchZoneUpdate = new ZoneUpdate
        {
            Statistics = new MatchStats
            {
                PlayerStats = _zoneData.PlayerStats
            },
            PlayerSpawnPoints = _zoneData.PlayerSpawnPoints
        };
        
        _serviceZone.SendUpdateZone(matchZoneUpdate);
        
        var playerUnit = _playerUnits.FirstOrDefault(p => p.Value.PlayerId == playerId).Value;
        PlayerMovementActive(playerUnit);
    }

    public void PlayerLeft(uint playerId)
    {
        var unitId = _playerIdToUnitId[playerId];
        _playerUnits.Remove(unitId);
        _units.Remove(unitId);
        _playerIdToUnitId.Remove(playerId);
        _zoneData.PlayerStats.Remove(playerId);
        _zoneData.PlayerSpawnPoints.Remove(playerId);
        _zoneData.PlayerInfo.Remove(playerId);
        
        _serviceZone.SendKill(new KillInfo
        {
            DeadUnitId = unitId,
            Dead = null,
            Killer = null,
            Assistants = [],
            DamageSource = Key.None,
            SourcePosition = Vector3.Zero,
            Crit = false
        });
        
        _serviceZone.SendUpdateZone(new ZoneUpdate
        {
            PlayerInfo = _zoneData.PlayerInfo,
            PlayerSpawnPoints = _zoneData.PlayerSpawnPoints,
            Statistics = new MatchStats
            {
                PlayerStats = _zoneData.PlayerStats,
                Team1Stats = _zoneData.GetTeamScores(TeamType.Team1),
                Team2Stats = _zoneData.GetTeamScores(TeamType.Team2)
            }
        });
    }

    private void PlayerMovementActive(Unit playerUnit)
    {
        var startMovement = new UnitUpdate
        {
            MovementActive = true
        };
        playerUnit.UpdateData(startMovement);
        _serviceZone.SendUnitUpdate(playerUnit.Id, startMovement);
    }

    private static List<BarrierLabel> GetBarriersForPhase(ZonePhaseType phase)
    {
        return phase switch
        {
            ZonePhaseType.Waiting => [],
            ZonePhaseType.TutorialInit => [],
            ZonePhaseType.Build => [BarrierLabel.Build1Team1, BarrierLabel.Build1Team2],
            ZonePhaseType.Assault => [],
            ZonePhaseType.Build2 => [BarrierLabel.Build2Team1, BarrierLabel.Build2Team2],
            ZonePhaseType.Assault2 => [],
            ZonePhaseType.SuddenDeath => [],
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
        };
    }

    private async Task RunGameLoop()
    {
        var tickTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(Tick.DeltaMillis));
        var token = GameCanceler.Token;
        while (await tickTimer.WaitForNextTickAsync(token))
        {
            EnqueueAction(FlushBuffer);
        }
    }

    private void FlushBuffer()
    {
        var buffer = _sendBuffer.GetBuffer();
        if (buffer.Length > 0)
           _sessionsSender.Send(buffer);
    }
    
    private void OnBuild1TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_build1Timer == null) return;
        _build1Timer.Stop();
        _build1Timer.Dispose();
        _build1Timer = null;

        EnqueueAction(UpdatePhase);
    }

    private void OnBuild2TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_build2Timer == null) return;
        _build2Timer.Stop();
        _build2Timer.Dispose();
        _build2Timer = null;

        EnqueueAction(UpdatePhase);
    }
}