using System.Numerics;
using System.Timers;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.Octree_Extensions;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;
using BNLReloadedServer.Service;
using Octree;
using MatchType = BNLReloadedServer.BaseTypes.MatchType;
using Timer = System.Timers.Timer;

namespace BNLReloadedServer.ServerTypes;

public partial class GameZone : Updater
{
    private const int TickRate = Tick.DeltaMillis / 2;
    private const float TicksPerSecond = 1000f / TickRate;
    private const float SecondsPerTick = 1f / TicksPerSecond;
    private const int TicksForBuffCheck = 200 / TickRate;
    private const float BuffMultiplier = SecondsPerTick * TicksForBuffCheck;
    
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

    private readonly BoundsOctreeEx<Unit> _unitOctree;

    private readonly HashSet<ConstEffectInfo>[]
        _teamEffects = new HashSet<ConstEffectInfo>[Enum.GetValues<TeamType>().Length];
    
    private readonly Dictionary<uint, MapSpawnPoint> _mapSpawnPoints = new();
    private readonly uint[] _defaultSpawnId = new uint[Enum.GetValues<TeamType>().Length];
    private readonly Queue<UnitLabel>[] _objectiveConquest = new Queue<UnitLabel>[Enum.GetValues<TeamType>().Length];
    
    private readonly Dictionary<ulong, ShotInfo> _shotInfo = new();
    private readonly HashSet<ulong> _keepShotAlive = [];
    
    private Task? _gameLoop;

    private Timer? _build1Timer;
    private Timer? _build2Timer;

    private readonly UnitUpdater _defaultUnitUpdater;

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

        _defaultUnitUpdater = new UnitUpdater(GetUnitInitAction(),
            UnitUpdated,
            UnitMoved,
            UnitTeamEffectAdded,
            UnitTeamEffectRemoved,
            ApplyInstEffect,
            GetTeamEffects,
            DoesObjBuffApply, 
            ImpactOccur,
            GetResourceCap,
            UpdateMatchStats);
        
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
        var startingPhase = match?.Data?.Type is MatchType.ShieldCapture or MatchType.ShieldRush2
            ? ZonePhaseType.Waiting
            : ZonePhaseType.TutorialInit;

        for (var index = 0; index < _teamEffects.Length; index++)
        {
            _teamEffects[index] = [];
        }

        _zoneData = new ZoneData(new ZoneUpdater(ZoneUpdated))
        {
            MatchKey = match.Key,
            GameModeKey = gameInitiator.GetGameMode(),
            MapData = mapData,
            MapKey = mapKey,
            BlocksData = new MapBinary(mapData.Schema, mapData.BlocksData ?? MapLoader.GetBlockData(mapKey) ?? [],
                mapData.Size, mapData.Properties?.PlanePosition ?? 0, new MapUpdater(OnCut)),
            CanSwitchHero = gameInitiator.CanSwitchHero(),
            Phase = new ZonePhase
            {
                PhaseType = startingPhase,
                StartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            }
        };

        _unitOctree = new BoundsOctreeEx<Unit>(
            Math.Max(Math.Max(mapData.Size.x, mapData.Size.y), mapData.Size.z),
            (mapData.Size / 2).ToVector3(),
            1,
            1.2f);

        _zoneData.BlocksData.Units = _unitOctree;
        
        BeginningZoneInitData = _zoneData.GetZoneInitData();
        EnqueueAction(() =>
        {
            _zoneData.SpawnPoints = spawns;
            _zoneData.PlayerInfo = playerMap;
            _zoneData.ResourceCap = gameInitiator.GetResourceCap();
            SetUpObjectives();
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
                player.Value.ZoneService = zoneService;
                zoneService.SendUnitCreate(player.Key, init);
            }
            else
            {
                zoneService.SendUnitCreate(player.Key, player.Value.GetInitData());
            }
            zoneService.SendUnitUpdate(player.Key, player.Value.GetUpdateData());
        }

        if (_playerUnits.Values.Any(player => player.PlayerId == playerId) || _gameInitiator.IsPlayerSpectator(playerId)) return;
        var playerUnit = CreatePlayerUnit(playerId, zoneService);
        if (playerUnit == null) return;
        playerUnit.ZoneService = zoneService;
        zoneService.SendUnitUpdate(playerUnit.Id, playerUnit.GetUpdateData());
    }

    private Unit? CreatePlayerUnit(uint playerId, IServiceZone creatorService)
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
        
        return CatalogueFactory.CreatePlayerUnit(unitId, playerInfo.PlayerId, transform, playerInfo, _gameInitiator,
            _defaultUnitUpdater with { OnUnitInit = GetUnitInitAction(creatorService) });
    }
    
    // Map units are controlled by everyone in the match
    private void CreateMapUnits()
    {
        foreach (var unit in _zoneData.MapData.Units)
        {
            var unitId = NewUnitId;
            CatalogueFactory.CreateUnit(unitId, unit, _defaultUnitUpdater);
        }
    }

    private void CreateUnit(CardUnit unit, ZoneTransform transform, Unit? builder = null, IServiceZone? creatorService = null)
    {
        var updater = creatorService != null ? _defaultUnitUpdater with { OnUnitInit = GetUnitInitAction(creatorService) } : _defaultUnitUpdater;
        CatalogueFactory.CreateUnit(NewUnitId, unit.Key, transform, builder?.Team ?? TeamType.Neutral, builder, updater);
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
                if (_zoneData.MapData.Units.Any(unit => (Databases.Catalogue.GetCard<CardUnit>(unit.UnitKey)?.Labels?.Contains(objLabel) ?? false) && unit.Team == team))
                {
                    _objectiveConquest[(int) team].Enqueue(objLabel);                                        
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

        var phaseUpdate = new ZoneUpdate
        {
            Phase = new ZonePhase
            {
                PhaseType = nextPhase,
                StartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                EndTime = endTime,
            }
        };
        
        _zoneData.UpdateData(phaseUpdate);
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
            PlayerSpawnPoints = spawnPoints
        };

        _zoneData.UpdateData(matchZoneUpdate);
        
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
                PlayerStats = _zoneData.PlayerStats,
                Team1Stats = _zoneData.GetTeamScores(TeamType.Team1),
                Team2Stats = _zoneData.GetTeamScores(TeamType.Team2)
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
        RemoveUnit(unitId);
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

    private void AddUnitToOctree(Unit unit, ZoneTransform transform)
    {
        if (unit.UnitCard?.Size is not { } size) return;
        if (unit.PlayerId != null)
        {
            var playerSize = new Vector3(0.5f, 1.9f, 0.5f);
            if (transform.IsCrouch)
            {
                playerSize.Y = 0.9f;
            }
            _unitOctree.Add(unit, new BoundingBox(transform.Position, playerSize - UnitSizeHelper.ImprecisionVector));
        }
        else
        {
            _unitOctree.Add(unit, new BoundingBox(transform.Position, size.ToVector3() - UnitSizeHelper.ImprecisionVector));
        }
    }

    private void RemoveUnit(uint unitId)
    {
        _unitOctree.Remove(_units[unitId]);
        _units.Remove(unitId);
    }

    private static void PlayerMovementActive(Unit playerUnit) => playerUnit.UpdateData(new UnitUpdate { MovementActive = true });

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
        var tickTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(TickRate));
        var token = GameCanceler.Token;
        var tickNumber = 0UL;
        while (await tickTimer.WaitForNextTickAsync(token)) EnqueueAction(OnTick(tickNumber++));
    }

    private Action OnTick(ulong tickNumber) =>
        () =>
        {
            var doBuffCheck = tickNumber % TicksForBuffCheck == 0;
            foreach (var (_, unit) in _units)
            {
                unit.RemoveExpiredEffects();
                foreach (var (aura, bounds) in unit.AuraEffects)
                {
                    var previousColliders = unit.UnitsInAuraSinceLastUpdate.GetValueOrDefault(aura, []);
                    var currentColliders = _unitOctree.GetColliding(bounds);
                    var exiting = previousColliders.Except(currentColliders).ToList();
                    var entering = currentColliders.Except(previousColliders).ToList();
                    unit.UnitsInAuraSinceLastUpdate[aura] = currentColliders;
                    if (exiting.Count != 0)
                    {
                        if (aura.LeaveEffect != null)
                        {
                            ApplyInstEffect(unit, exiting, aura.LeaveEffect, false, unit.CreateImpactData());
                        }

                        if (aura.ConstantEffects != null)
                        {
                            exiting.ForEach(u => u.RemoveEffects(aura.ConstantEffects.Select(e => new ConstEffectInfo(e)).ToList(), unit.Team));
                        }
                    }

                    if (entering.Count != 0)
                    {
                        if (aura.EnterEffect != null)
                        {
                            ApplyInstEffect(unit, entering, aura.EnterEffect, false, unit.CreateImpactData());
                        }

                        if (aura.ConstantEffects != null)
                        {
                            entering.ForEach(u => u.AddEffects(aura.ConstantEffects.Select(e => new ConstEffectInfo(e)).ToList(), unit.Team));
                        }
                    }
                }

                foreach (var (nearby, bounds) in unit.NearbyBlockEffects)
                {
                    var nearbyIds = nearby.Blocks?
                        .Select(key => Databases.Catalogue.GetCard<CardBlock>(key)?.BlockId)
                        .OfType<ushort>()
                        .ToList();
                    if (nearbyIds is not { Count: > 0 } || nearby.Effects == null) continue;
                    if(_zoneData.BlocksData.CheckBlocks(bounds, block => nearbyIds.Contains(block.Id)))
                    {
                        unit.AddEffects(nearby.Effects.Select(e => new ConstEffectInfo(e)).ToList(), unit.Team);
                    }
                    else
                    {
                        unit.RemoveEffects(nearby.Effects.Select(e => new ConstEffectInfo(e)).ToList(), unit.Team);
                    }
                }

                if (doBuffCheck)
                {
                    unit.ApplyBuffEffects(BuffMultiplier);
                }
            }
            
            FlushBuffer();
        };

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