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
    private const int TicksForDmgCaptureCheck = 1000 / TickRate;
    private const float BuffMultiplier = SecondsPerTick * TicksForBuffCheck;
    
    public CancellationTokenSource GameCanceler { get; } = new();
    
    private readonly ZoneData _zoneData;

    private MapBinary MapBinary => _zoneData.BlocksData;
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
    private readonly Dictionary<uint, Unit> _playerSpawnPoints = new();
    private readonly uint[] _defaultSpawnId = new uint[Enum.GetValues<TeamType>().Length];
    private readonly Queue<UnitLabel>[] _objectiveConquest = new Queue<UnitLabel>[Enum.GetValues<TeamType>().Length];
    private TeamType _winningTeam = TeamType.Neutral;
    
    private readonly Dictionary<ulong, ShotInfo> _shotInfo = new();
    private readonly HashSet<ulong> _keepShotAlive = [];
    
    private Task? _gameLoop;

    private Timer? _build1Timer;
    private Timer? _build2Timer;

    private float _respawnTime;
    private int increaseTimes;
    private Timer? _respawnIncreaseTimer;

    private readonly UnitUpdater _defaultUnitUpdater;

    private uint _newUnitId = 1;
    private uint _newSpawnId = 1;

    private uint NewUnitId() => _newUnitId++;
    private uint NewSpawnId() => _newSpawnId++;

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
            UpdateMatchStats,
            UnitIsDamaged,
            UnitIsKilled,
            DropUnit,
            LinkPortal,
            OnPull,
            UnitCreated,
            EnqueueAction);
        
        var spawns = new Dictionary<uint, SpawnPoint>();

        foreach (var spawnPoint in mapData.SpawnPoints)
        {
            var spawnId = NewSpawnId();
            spawns.Add(spawnId, new SpawnPoint
            {
                Id = spawnId,
                Team = spawnPoint.Team,
                Pos = spawnPoint.Position,
                Lock = IsMapSpawnRequirementsMet(spawnPoint),
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

        var match = CatalogueHelper.GetMatch(mapData.Match, gameInitiator.GetGameMode());
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
                mapData.Size, mapData.Properties?.PlanePosition ?? 0, new MapUpdater(OnCut, OnMined, OnDetached)),
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

        _respawnTime = _zoneData.MatchCard.RespawnLogic?.BaseRespawnTime ?? 10f;
        
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

    public void SendLoadZone(IServiceZone zoneService, IServiceZone savedService, uint playerId)
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
                player.Value.ZoneService = savedService;
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
        playerUnit.ZoneService = savedService;
        zoneService.SendUnitUpdate(playerUnit.Id, playerUnit.GetUpdateData());
    }

    private Unit? CreatePlayerUnit(uint playerId, IServiceZone creatorService)
    {
        var playerInfo = _playerLobbyInfo.Find(player => player.PlayerId == playerId);
        if (playerInfo == null) return null;
        var spawnId = _defaultSpawnId[(int)playerInfo.Team];
        var spawnPoint = _mapSpawnPoints.GetValueOrDefault(spawnId);
        var pos = Vector3.Zero;
        var rot = Quaternion.Identity;
        if (spawnPoint != null)
        {
            pos = GetSpawnPosition(spawnPoint.Position, 2);
            rot = Direction2dHelper.Rotation(spawnPoint.Direction);
        }

        var transform = ZoneTransformHelper.ToZoneTransform(pos, rot);
        
        var unitId = NewUnitId();
        
        return CatalogueFactory.CreatePlayerUnit(unitId, playerInfo.PlayerId, transform, playerInfo, _gameInitiator, _zoneData.MatchCard,
            _defaultUnitUpdater with { OnUnitInit = GetUnitInitAction(creatorService) });
    }
    
    // Map units are controlled by everyone in the match
    private void CreateMapUnits()
    {
        foreach (var unit in _zoneData.MapData.Units)
        {
            var unitId = NewUnitId();
            CatalogueFactory.CreateUnit(unitId, unit, _defaultUnitUpdater);
        }
    }

    private Unit? CreateUnit(CardUnit unit, ZoneTransform transform, Unit? builder = null, IServiceZone? creatorService = null, bool isAttached = false)
    {
        var updater = creatorService != null
            ? _defaultUnitUpdater with { OnUnitInit = GetUnitInitAction(creatorService) }
            : _defaultUnitUpdater;
        var newUnit = CatalogueFactory.CreateUnit(NewUnitId(), unit.Key, transform, builder?.Team ?? TeamType.Neutral,
            builder, updater, isAttached: isAttached);
        if (newUnit == null) return newUnit;
        
        if (unit.CountLimit is { Limit: > 0 })
        {
            var existingUnits = unit.CountLimit.Scope switch
            {
                UnitLimitScope.World => _units.Values.Where(u => u.Key == unit.Key).ToList(),
                
                UnitLimitScope.Team => _units.Values.Where(u => u.Key == unit.Key && u.Team == newUnit.Team).ToList(),
                
                UnitLimitScope.Owner when newUnit.OwnerPlayerId is not null => _units.Values
                    .Where(u => u.Key == unit.Key && u.OwnerPlayerId == newUnit.OwnerPlayerId)
                    .ToList(),
                
                _ => []
            };
            
            if (existingUnits.Count > unit.CountLimit.Limit)
            {
                existingUnits.Sort((u1, u2) => u1.CreationTime.CompareTo(u2.CreationTime));
                existingUnits[unit.CountLimit.Limit - 1].Killed(existingUnits[unit.CountLimit.Limit - 1].CreateBlankImpactData());
            }
        }

        if (unit.Data is not UnitDataPortal || newUnit.OwnerPlayerId is null) return newUnit;
        
        LinkPortal(newUnit);
        
        return newUnit;
    }
    
    private void CreateLootUnit(LootItemUnit loot, ZoneTransform transform, Unit? killer = null)
    {
        var lootCard = Databases.Catalogue.GetCard<CardUnit>(loot.LootUnitKey);
        if (lootCard == null) return;
        var team = loot.KillerRelativeLootTeam switch
        {
            RelativeTeamType.Both => TeamType.Neutral,
            RelativeTeamType.Friendly => killer?.Team ?? TeamType.Neutral,
            RelativeTeamType.Opponent when killer?.Team == TeamType.Team1 => TeamType.Team2,
            RelativeTeamType.Opponent when killer?.Team == TeamType.Team2 => TeamType.Team1,
            _ => TeamType.Neutral
        };
        CatalogueFactory.CreateUnit(NewUnitId(), loot.LootUnitKey, transform, team, null, _defaultUnitUpdater);
    }

    private void CreateProjectileUnit(Key projectileKey, float speed, ShotData shot, Vector3 shotPos,
        Unit? creator = null)
    {
        var updater = creator?.ZoneService != null
            ? _defaultUnitUpdater with { OnUnitInit = GetUnitInitAction(creator.ZoneService) }
            : _defaultUnitUpdater;
        
        var vecDir = shot.TargetPos - shotPos;
        var transform = ZoneTransformHelper.ToZoneTransform(shotPos, QuaternionExtensions.LookRotation(vecDir));
        transform.SetLocalVelocity(Vector3.Normalize(vecDir) * speed);
        
        CatalogueFactory.CreateUnit(NewUnitId(), projectileKey, transform, creator?.Team ?? TeamType.Neutral, creator, updater, speed);
    }

    private Vector3 GetSpawnPosition(Vector3 spawnPoint, float spawnRadius)
    {
        if (spawnRadius < 1) return spawnPoint;
        
        var spawnBlocks = (int)float.Floor(spawnRadius);
        var blockedPositions = MapBinary.GetContainedInUnits(
            _unitOctree.GetColliding(new BoundingBoxEx(spawnPoint, new Vector3(spawnBlocks * 2, 2, spawnBlocks * 2)))
                .Where(u => u.UnitCard?.Labels?.Contains(UnitLabel.RespawnPoint) is not true).ToList());

        List<Vector3> validPositions = [];
        for (var x = spawnPoint.X - spawnBlocks; x <= spawnPoint.X + spawnBlocks; x++)
        {
            for (var z = spawnPoint.Z - spawnBlocks; z <= spawnPoint.Z + spawnBlocks; z++)
            {
                var pos = new Vector3(x, spawnPoint.Y, z);
                if (!blockedPositions.Contains((Vector3s)pos) &&
                    MapBinary[(Vector3s)pos].Card.Passable is BlockPassableType.Any &&
                    (!MapBinary.ContainsBlock((Vector3s)(pos + Vector3.UnitY)) ||
                     MapBinary[(Vector3s)(pos + Vector3.UnitY)].Card.Passable is BlockPassableType.Any))
                {
                    validPositions.Add(pos);
                }
            }
        }
        
        var rand = new Random();
        if (validPositions.Count != 0) return rand.GetItems(validPositions.ToArray(), 1)[0];
        
        var spawnX = rand.Next(-spawnBlocks, spawnBlocks + 1);
        var spawnZ = rand.Next(-spawnBlocks, spawnBlocks + 1);
        
        return spawnPoint + new Vector3(spawnX, 0, spawnZ);
    }

    private SpawnPointLockType IsMapSpawnRequirementsMet(MapSpawnPoint spawnPoint)
    {
        if (spawnPoint.Label != SpawnPointLabel.Objective1) return SpawnPointLockType.Free;
        
        if (_objectiveConquest[(int)spawnPoint.Team].TryPeek(out var currObj))
        {
            return currObj != UnitLabel.Line1 ? SpawnPointLockType.Free : SpawnPointLockType.ServerBlocked;
        }
            
        return SpawnPointLockType.ServerBlocked;
    }

    private SpawnPointLockType CheckSpawn(Unit unit, Vector3 spawnPoint)
    {
        if (unit.IsBuff(BuffType.Disabled))
        {
            return SpawnPointLockType.ServerBlocked;
        }
        
        for (var y = 0; y <= 1; y++){
            var pos = new Vector3s(spawnPoint.X, spawnPoint.Y + y, spawnPoint.Z);
            if (MapBinary.ContainsBlock(pos) && MapBinary[pos].Card.Passable != BlockPassableType.Any)
            {
                return SpawnPointLockType.WorldBlocked;
            }
        }
        
        var spawnZonePoint = spawnPoint with { Y = float.Ceiling(spawnPoint.Y) };
        var spawnZone = new BoundingBoxEx(spawnZonePoint, new Vector3(1, 2, 1) - UnitSizeHelper.ImprecisionVector);

        var colliding = _unitOctree.GetColliding(spawnZone).Except([unit]).ToList();

        if (colliding.Count != 0)
        {
            return colliding.Any(u => u.PlayerId != null)
                ? SpawnPointLockType.PlayerBlocked
                : SpawnPointLockType.WorldBlocked;
        }

        return SpawnPointLockType.Free;
    }

    private float GetRespawnLength() => _respawnTime * (1 + _gameInitiator.GetRespawnMultiplier());

    private void UpdateRespawnTime(Unit unit)
    {
        if (unit.PlayerId == null) return;
        var respLength = GetRespawnLength();
        unit.RespawnTime = DateTimeOffset.Now.AddSeconds(respLength);
        _zoneData.UpdateSpawnTime(unit.PlayerId.Value, (ulong)unit.RespawnTime.Value.ToUnixTimeMilliseconds());
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
                var i = 0;
                var respTimes = _zoneData.MatchCard.RespawnLogic?.IncrementSequence;
                while (i < respTimes?.Count && respTimes[i].MatchSeconds == 0)
                {
                    IncreaseSpawnTime();
                    i++;
                }

                if (i < respTimes?.Count)
                {
                    _respawnIncreaseTimer = new Timer(TimeSpan.FromSeconds(respTimes[i].MatchSeconds).TotalMilliseconds);
                    _respawnIncreaseTimer.Elapsed += OnRespawnTimerIncreased;
                    _respawnIncreaseTimer.AutoReset = false;
                    _respawnIncreaseTimer.Start();
                }
                else if (_zoneData.MatchCard.RespawnLogic?.IncrementRepeatSequence?.Count > 0)
                {
                    _respawnIncreaseTimer = new Timer(TimeSpan
                        .FromSeconds(_zoneData.MatchCard.RespawnLogic.IncrementRepeatSequence[0].MatchSeconds)
                        .TotalMilliseconds);
                    _respawnIncreaseTimer.Elapsed += OnRespawnTimerIncreased;
                    _respawnIncreaseTimer.AutoReset = false;
                    _respawnIncreaseTimer.Start();
                }
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
        
        foreach (var unit in _units.Values)
        {
            var uCard = unit.UnitCard;
            if (uCard?.BlockBinding is UnitBlockBindingType.Detach or UnitBlockBindingType.Destroy)
            {
                var attachedFace = CoordsHelper.RotationToFace(unit.Transform.Rotation);
                var attachedBlockPos =
                    (Vector3s)(CoordsHelper.FaceToVector[(int)CoordsHelper.OppositeFace[(int)attachedFace]]
                        .ToVector3() * Math.Max(((uCard.Size?.y ?? 1) - 1) / 2.0f + 1, 1) + unit.GetMidpoint());
                if (MapBinary.ContainsBlock(attachedBlockPos))
                {
                    MapBinary.AttachToBlock(unit, attachedBlockPos, attachedFace);
                }
                else if (uCard.Movement is UnitMovementCustom or UnitMovementFalling)
                {
                    MovementActive(unit);
                }
                
            }
            else if (uCard?.Movement is UnitMovementCustom or UnitMovementFalling)
            { 
                MovementActive(unit); 
            }
        }

        _gameLoop = RunGameLoop();
    }

    public void IncreaseSpawnTime(Timer? timer = null)
    {
        var respTimes = _zoneData.MatchCard.RespawnLogic?.IncrementSequence;
        var repRespTimes = _zoneData.MatchCard.RespawnLogic?.IncrementRepeatSequence;
        if (respTimes is { Count: > 0 } && increaseTimes < respTimes.Count)
        {
            var resp = respTimes[increaseTimes];
            _respawnTime += resp.RespawnTimeIncSeconds;
            increaseTimes++;
            if (timer is not null)
            {
                timer.Interval = TimeSpan.FromSeconds(resp.MatchSeconds).TotalMilliseconds;
            }
        }
        else if (repRespTimes is { Count: > 0 } && (_zoneData.MatchCard.RespawnLogic?.IncrementRepeatLimit is null ||
                                                    increaseTimes - (respTimes?.Count ?? 0) <
                                                    _zoneData.MatchCard.RespawnLogic.IncrementRepeatLimit))
        {
            var incTimes = increaseTimes - (respTimes?.Count ?? 0);
            var resp = repRespTimes[incTimes % repRespTimes.Count];
            _respawnTime += resp.RespawnTimeIncSeconds;
            increaseTimes++;
            if (timer is not null)
            {
                timer.Interval = TimeSpan.FromSeconds(resp.MatchSeconds).TotalMilliseconds;
            }
        }
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
        MovementActive(playerUnit);
    }

    public void PlayerLeft(uint playerId)
    {
        var unitId = _playerIdToUnitId[playerId];
        var player = _playerUnits[unitId];
        var impact = new ImpactData
        {
            Crit = false,
            InsidePoint = player.GetMidpoint(),
            ShotPos = player.GetMidpoint(),
            Normal = Vector3s.Zero
        };
        player.Killed(impact);

        foreach (var unit in _units.Values.Where(u => u.OwnerPlayerId == playerId).ToList())
        {
            impact.InsidePoint = unit.GetMidpoint();
            impact.ShotPos = unit.GetMidpoint();
            unit.Killed(impact);
        }

        var blkUpdates = new Dictionary<Vector3s, BlockUpdate>();
        foreach (var block in MapBinary.OwnedBlocks.Where(b =>
                     MapBinary[b.Key].Card.DeviceType == DeviceType.Device && b.Value.OwnerPlayerId == playerId))
        {
            foreach (var update in MapBinary.RemoveBlock(block.Key))
            {
                blkUpdates[update.Key] = update.Value;
            }
        }

        if (blkUpdates.Count > 0)
        { 
            _unbufferedZone.SendBlockUpdates(blkUpdates);
        }
        
        _playerUnits.Remove(unitId);
        RemoveUnit(unitId);
        _playerIdToUnitId.Remove(playerId);
        _zoneData.PlayerStats.Remove(playerId);
        _zoneData.PlayerSpawnPoints.Remove(playerId);
        _zoneData.PlayerInfo.Remove(playerId);
        _zoneData.RespawnInfo.Remove(playerId);
        
        _serviceZone.SendUpdateZone(new ZoneUpdate
        {
            PlayerInfo = _zoneData.PlayerInfo,
            PlayerSpawnPoints = _zoneData.PlayerSpawnPoints,
            Statistics = new MatchStats
            {
                PlayerStats = _zoneData.PlayerStats,
                Team1Stats = _zoneData.GetTeamScores(TeamType.Team1),
                Team2Stats = _zoneData.GetTeamScores(TeamType.Team2)
            },
            RespawnInfo = _zoneData.RespawnInfo
        });
    }

    private Unit[] CollidingWithUnit(Unit unit, Vector3? position = null)
    {
        if (unit.UnitCard?.Size is not { } size) return [];
        position ??= unit.GetMidpoint();
        Unit[] colliding;
        if (unit.PlayerId == null)
        {
            colliding = _unitOctree.GetColliding(new BoundingBoxEx(position.Value,
                            size.ToVector3() - UnitSizeHelper.ImprecisionVector));
            return colliding.Where(u => u.Id != unit.Id && u.Key != CatalogueHelper.SmokeBomb).ToArray();
        }
        
        var playerSize = new Vector3(0.5f, 1.9f, 0.5f);
        if (unit.Transform.IsCrouch)
        {
            playerSize.Y = 0.9f;
        }
        
        colliding = _unitOctree.GetColliding(new BoundingBoxEx(position.Value, playerSize - UnitSizeHelper.ImprecisionVector));
        return colliding.Where(u => u.Id != unit.Id && u.Key != CatalogueHelper.SmokeBomb).ToArray();
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
            _unitOctree.Add(unit, new BoundingBox(unit.GetMidpoint(transform.Position), playerSize - UnitSizeHelper.ImprecisionVector));
        }
        else
        {
            _unitOctree.Add(unit, new BoundingBox(unit.GetMidpoint(transform.Position), size.ToVector3() - UnitSizeHelper.ImprecisionVector));
        }
    }

    private void RemoveUnit(uint unitId)
    {
        RemoveUnitFromOctree(unitId);
        _units.Remove(unitId);
    }
    
    private void RemoveUnitFromOctree(uint unitId)
    {
        if (_units.TryGetValue(unitId, out var unit))
            _unitOctree.Remove(unit);
    }

    private static void MovementActive(Unit unit) => unit.UpdateData(new UnitUpdate { MovementActive = true });

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

    private const float CubeMult = 1;
    private const float PlayerMult = 0.5f;
    private const float WinningThreshold = 1;
    private TeamType GetWinningTeam()
    {
        if (_zoneData.MatchCard.Data?.Type is MatchType.TimeTrial or MatchType.Tutorial or null)
            return TeamType.Neutral;
        
        var cubeWinFactor = (_objectiveConquest[1].Count - _objectiveConquest[2].Count) * CubeMult;
        var playerWinFactor = _playerUnits.Values.Aggregate(0,
            (score, player) => score + (player.IsDead ? 0 : player.Team is TeamType.Team1 ? 1 : -1),
            score => score * PlayerMult);
        
        var totalWinFactor = cubeWinFactor + playerWinFactor;

        return totalWinFactor switch
        {
            >= WinningThreshold => TeamType.Team1,
            <= -WinningThreshold => TeamType.Team2,
            _ => TeamType.Neutral
        };
    }

    private DamageData ConvertToDamageData(Damage damage, Vector3 targetPos, Vector3 shotPos, Unit? source = null,
        bool splashDamage = false, bool crit = false, 
        float critMultiplier = 1f, DamageFalloff? falloff = null)
    {
        var friendlyFire = _zoneData.MatchCard.FriendlyFire;
        var playerDmg = source?.PlayerDamageAmount(damage.PlayerDamage) ?? damage.PlayerDamage;
        if (crit)
        {
            playerDmg *= critMultiplier;
        }
        
        var worldDmg = damage is { Mining: true }
            ? source?.ToolWorldDamageAmount(damage.WorldDamage) ?? damage.WorldDamage
            : source?.WorldDamageAmount(damage.WorldDamage) ?? damage.WorldDamage;
        
        var objDmg = source?.ObjectiveDamageAmount(damage.ObjectiveDamage) ?? damage.ObjectiveDamage;

        Func<float, float> falloffFunc = dmg => dmg;
        if (falloff is { MaxDamageRange: >= 0 } && falloff.MaxDamageRange < falloff.MinDamageRange)
        {
            var dist = Vector3.Distance(targetPos, shotPos);
            var falloffCoef = float.Max(
                (dist - falloff.MaxDamageRange) /
                (falloff.MinDamageRange - falloff.MaxDamageRange), 0);  
            falloffFunc = dmg =>
                falloffCoef > 1
                    ? dist > falloff.MaxRange ? 0f : dmg * falloff.ReductionCoeff
                    : float.Lerp(dmg, dmg * falloff.ReductionCoeff, falloffCoef);
        }
        
        var selfDmg = falloffFunc(friendlyFire is not null
            ? splashDamage
                ? (1 - friendlyFire.SelfSplashDamageReduction) * playerDmg
                : (1 - friendlyFire.DirectDamageReduction) * playerDmg
            : playerDmg);
        
        var teamDmg = falloffFunc(friendlyFire is not null
            ? splashDamage
                ? (1 - friendlyFire.SplashDamageReduction) * playerDmg
                : (1 - friendlyFire.DirectDamageReduction) * playerDmg
            : playerDmg);

        var teamDeviceDmg = falloffFunc(friendlyFire is not null
            ? damage.Mining
                ? (1 - friendlyFire.DevicesMiningDamageReduction) * worldDmg
                : splashDamage
                    ? (1 - friendlyFire.DevicesSplashDamageReduction) * worldDmg
                    : (1 - friendlyFire.DevicesDirectDamageReduction) * worldDmg
            : worldDmg);

        var teamObjDmg = falloffFunc(friendlyFire is not null
            ? (1 - friendlyFire.ObjectivesDamageReduction) * objDmg
            : objDmg);

        return new DamageData(selfDmg, teamDmg, falloffFunc(playerDmg), teamDeviceDmg, falloffFunc(worldDmg),
            falloffFunc(worldDmg), teamObjDmg,
            falloffFunc(objDmg), damage.Mining, damage.Melee, damage.IgnoreInvincibility, damage.IgnoreDefences);
    }

    private void RunBlockCheckForUnit(Unit unit)
    {
        var newMapBlocks = MapBinary.GetContainedInUnit(unit, 0);
        var newInsideBlocks = newMapBlocks.Where(b =>
            MapBinary[b].Card.Special is BlockSpecialInsideEffect && MapBinary.GetIsActuallyInside(unit, b)).ToList();
        var exitingBlocks = unit.OverlappingMapBlocks.Except(newInsideBlocks);
        unit.UpdateMapBlocks(newMapBlocks);
        
        foreach (var blk in newInsideBlocks)
        {
            var block = MapBinary[blk];
            if (block.Card.Special is not BlockSpecialInsideEffect insideEffect) continue;
                
            if (insideEffect.TriggerTeam switch
                {
                    RelativeTeamType.Friendly => block.Team != unit.Team,
                    RelativeTeamType.Opponent => block.Team == unit.Team,
                    _ => false
                }) continue;
            
            if (!MapBinary.UnitsInsideBlock.TryGetValue(blk, out var value))
            {
                value = new BlockIntervalUpdater(insideEffect, new BlockSource(blk, MapBinary[blk].ToBlock()));
                MapBinary.UnitsInsideBlock.Add(blk, value);
            }
            
            if(!value.AddUnit(unit)) continue;
                
            var (max, min) = UnitSizeHelper.GetExactUnitBounds(unit);
            var blockPos = blk.ToVector3();
            var blockImpact = MapBinary.CreateImpactForBlock(blk,
                Vector3.Clamp(Vector3.Clamp(CoordsHelper.BlockBottom(blk), min, max), blockPos + UnitSizeHelper.ImprecisionVector,
                    blockPos + Vector3.One - UnitSizeHelper.ImprecisionVector));
            var blockSource = new BlockSource(blk, MapBinary[blk].ToBlock(), blockImpact);
            
            if (insideEffect.InsideEffects is { Count: > 0 } effects)
            {
                unit.AddEffects(effects.Select(eff => new ConstEffectInfo(eff)).ToList(), blockSource.Team, blockSource);
            }

            if (insideEffect.EnterEffect?.Effect is null) continue;
            
            if (insideEffect.EnterEffect.TargetUnit)
            {
                ApplyInstEffect(blockSource, [unit], insideEffect.EnterEffect.Effect, blockImpact,
                    damageBlock: insideEffect.EnterEffect.TargetSelf);
            }
            else if (insideEffect.EnterEffect.TargetSelf)
            {
                ApplyInstEffect(blockSource, [], insideEffect.EnterEffect.Effect, blockImpact,
                    damageBlock: true);
            }
        }

        foreach (var blk in exitingBlocks)
        {
            if (!MapBinary.UnitsInsideBlock.TryGetValue(blk, out var value)) continue;
            value.RemoveUnit(unit);
            if (value.Count <= 0)
            {
                MapBinary.UnitsInsideBlock.Remove(blk);
            }
        }
    }

    private async Task RunGameLoop()
    {
        var tickTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(TickRate));
        var token = GameCanceler.Token;
        var tickNumber = 0UL;
        EnqueueAction(() =>
        {
            foreach (var mapSpawnPoint in
                     _mapSpawnPoints.Where(s => s.Value.Label is SpawnPointLabel.Objective1))
            {
                _zoneData.UpdateSpawn(mapSpawnPoint.Key, IsMapSpawnRequirementsMet(mapSpawnPoint.Value));
            }
        });
        while (await tickTimer.WaitForNextTickAsync(token)) EnqueueAction(OnTick(tickNumber++));
    }

    private Action OnTick(ulong tickNumber) =>
        () =>
        {
            var doBuffCheck = tickNumber % TicksForBuffCheck == 0;
            var doDmgCaptureCheck = tickNumber % TicksForDmgCaptureCheck == 0;
            var doBlockCheck = tickNumber == 0;
            
            if (doBuffCheck)
            {
                _winningTeam = GetWinningTeam();
            }
            
            foreach (var unit in _units.Values.ToList())
            {
                if (doBlockCheck)
                {
                    RunBlockCheckForUnit(unit);
                }
                
                unit.CleanUpExpired();
                var unitSource = unit.GetSelfSource(unit.CreateImpactData());

                if (unit is { CurrentChannelData: { } channelData, TicksPerChannel: > 0 } &&
                    tickNumber % unit.TicksPerChannel == 0)
                {
                    if (unit.CurrentGear?.Tools[channelData.ToolIndex] is { Tool: ToolChannel channel } currToolLogic)
                    {
                        if (currToolLogic.IsEnoughAmmoToUse())
                        {
                            if (channel.IntervalEffects is { Count: > 0 })
                            {
                                var channelImpact = unit.CreateImpactData(insidePoint: channelData.HitPos, sourceKey: unit.CurrentGear.Key);
                                channel.IntervalEffects.ForEach(inst => 
                                    ApplyInstEffect(unitSource,
                                        channelData.TargetUnit.HasValue ? [_units[channelData.TargetUnit.Value]] : [],
                                        inst, channelImpact));
                            }

                            var ammoUpdate = currToolLogic.TakeAmmoUpdate();
                            if (ammoUpdate is not null)
                            {
                                unit.UpdateData(new UnitUpdate
                                {
                                    Ammo = new Dictionary<Key, List<Ammo>> { {unit.CurrentGear.Key, [ammoUpdate]} }
                                });
                            }
                        }
                        else if (unit.PlayerId.HasValue)
                        {
                            ReceivedEndChannelRequest(unit.PlayerId.Value);
                        }
                    }
                }
                
                foreach (var (aura, bounds) in unit.AuraEffects)
                {
                    var previousColliders = unit.UnitsInAuraSinceLastUpdate.GetValueOrDefault(aura, []);
                    var currentColliders = _unitOctree.GetColliding(bounds);
                    var exiting = previousColliders.Except(currentColliders).ToList();
                    var entering = currentColliders.Except(previousColliders).ToList();
                    unit.UnitsInAuraSinceLastUpdate[aura] = currentColliders;
                    if (exiting.Count > 0)
                    {
                        if (aura.LeaveEffect != null)
                        {
                            ApplyInstEffect(unitSource, exiting, aura.LeaveEffect, unitSource.Impact!);
                        }

                        if (aura.ConstantEffects != null)
                        {
                            exiting.ForEach(u =>
                                u.RemoveEffects(aura.ConstantEffects.Select(e => new ConstEffectInfo(e)).ToList(),
                                    unit.Team, unitSource));
                        }
                    }

                    if (entering.Count > 0)
                    {
                        if (aura.EnterEffect != null)
                        {
                            ApplyInstEffect(unitSource, entering, aura.EnterEffect, unitSource.Impact!);
                        }

                        if (aura.ConstantEffects != null)
                        {
                            entering.ForEach(u =>
                                u.AddEffects(aura.ConstantEffects.Select(e => new ConstEffectInfo(e)).ToList(),
                                    unit.Team, unitSource));
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
                    var nearbyBlock = MapBinary.CheckBlocks(bounds, block => nearbyIds.Contains(block.Id));
                    if(nearbyBlock is not null)
                    {
                        var imp = MapBinary.CreateImpactForBlock(nearbyBlock.Value, unit.GetMidpoint());
                        unit.AddEffects(nearby.Effects.Select(e => new ConstEffectInfo(e)).ToList(), unit.Team,
                            new BlockSource(nearbyBlock.Value, MapBinary[nearbyBlock.Value].ToBlock(), imp));
                    }
                    else
                    {
                        unit.RemoveEffects(nearby.Effects.Select(e => new ConstEffectInfo(e)).ToList(), unit.Team, null,
                            true);
                    }
                }

                if (unit.SpawnId is not null)
                {
                    UpdateSpawnPoint(unit);
                }

                var isDisabled = unit.GetBuff(BuffType.Disabled) > 0;
                switch (unit.UnitCard?.Data)
                {
                    case UnitDataCloud { InsideEffects: not null } unitDataCloud when !isDisabled && unit.CloudEffect is not null:
                        var prevColliders = unit.CloudEffect.NearbyUnits;
                        var currColliders = _unitOctree.GetColliding(unit.CloudEffect.Shape);
                        var exit = prevColliders.Except(currColliders).ToList();
                        var enter = currColliders.Except(prevColliders).ToList();

                        unit.CloudEffect.NearbyUnits = currColliders;
                        if (exit.Count > 0)
                        {
                            exit.ForEach(u =>
                                u.RemoveEffects(
                                    unitDataCloud.InsideEffects.Select(e => new ConstEffectInfo(e)).ToList(), unit.Team,
                                    unitSource));
                        }
                        if (enter.Count > 0)
                        {
                            enter.ForEach(u =>
                                u.AddEffects(unitDataCloud.InsideEffects.Select(e => new ConstEffectInfo(e)).ToList(),
                                    unit.Team, unitSource));
                        }
                        break;
                    
                    case UnitDataDamageCapture unitDataDamageCapture when doDmgCaptureCheck && unit.DamageCaptureEffect is not null:
                        var nearby = _unitOctree.GetColliding(unit.DamageCaptureEffect.Shape);
                        var prevBaddies = unit.DamageCaptureEffect.NearbyUnits;
                        var nearbyBaddies = nearby.Where(u =>
                            (u.UnitCard?.Labels?.Contains(unitDataDamageCapture.CapturerLabel) ?? false) &&
                            u.Team != unit.Team).ToArray();
                        var exitBaddies = prevBaddies.Except(nearbyBaddies).ToList();
                        var enterBaddies = nearbyBaddies.Except(prevBaddies).ToList();

                        unit.DamageCaptureEffect.NearbyUnits = nearbyBaddies;
                        if (unitDataDamageCapture.ZoneEffects is not null)
                        {
                            if (exitBaddies.Count > 0)
                            {
                                exitBaddies.ForEach(u =>
                                    u.RemoveEffects(
                                        unitDataDamageCapture.ZoneEffects.Select(e => new ConstEffectInfo(e)).ToList(),
                                        unit.Team, unitSource));
                            }
                            if (enterBaddies.Count > 0)
                            {
                                enterBaddies.ForEach(u =>
                                    u.AddEffects(
                                        unitDataDamageCapture.ZoneEffects.Select(e => new ConstEffectInfo(e)).ToList(),
                                        unit.Team, unitSource));
                            }
                        }

                        if (enterBaddies.Count > 0 || exitBaddies.Count > 0)
                        {
                            unit.UpdateData(new UnitUpdate
                            {
                                DamageCapturers = nearbyBaddies.Select(u => u.Id).ToList()
                            });
                        }

                        for (var i = nearbyBaddies.Length; i > 0; i--)
                        {
                            if (!unitDataDamageCapture.DamagePerCapturer.TryGetValue(i, out var dmg)) continue;
                            var dData = new DamageData(0, 0, 0, 0, 0, 0, 0,
                                dmg, false, false, false, true);
                            foreach (var enemy in nearbyBaddies)
                            {
                                var enemyImpact = enemy.CreateImpactData(sourceKey: unitDataDamageCapture.DamageSource);
                                enemyImpact.Impact = unitDataDamageCapture.DamageImpact;
                                enemyImpact.HitUnits = [unit.Id];
                                unit.TakeDamage(dData, enemyImpact, false, enemy, null);
                            }
                            break;
                        }
                        break;
                    
                    case UnitDataDrill dataDrill:
                        if (dataDrill.HitsLimit <= unit.HitCount)
                        {
                            var impact = unit.CreateBlankImpactData();
                            unit.Killed(impact);
                        }
                        break;
                    
                    case UnitDataLandmine when !isDisabled && unit.LandmineEffect is not null:
                        var nearbyUnits = _unitOctree.GetColliding(unit.LandmineEffect.Shape);
                        var nearbyEnemies = nearbyUnits.Where(u => u.PlayerId != null && u.Team != unit.Team).ToList();
                        if (nearbyEnemies is { Count: > 0 })
                        {
                            unit.Killed(unit.CreateBlankImpactData());
                        }
                        break;
                    
                    case UnitDataPortal portalData when !isDisabled && unit.PortalLinked.LinkedPortalUnitId is not null:
                        var portalSize = unit.UnitCard.Size ?? Vector3s.Zero;
                        if (portalSize != Vector3s.Zero)
                        {
                            var otherPortal = _units.GetValueOrDefault(unit.PortalLinked.LinkedPortalUnitId.Value);
                            if (otherPortal is null) break;

                            var unitMidpoint = unit.GetMidpoint();
                            var teleportRange = new BoundingBoxEx(unitMidpoint,
                                new Vector3(0.5f, portalSize.y, 0.5f));
                            
                            var unitsForTeleport = _unitOctree.GetColliding(teleportRange).Where(u =>
                                u.Id != unit.Id && (portalData.UnitsFilter is not { } targeting || u.DoesEffectApply(targeting, unit.Team))).ToList();

                            if (unitsForTeleport.Count != 0)
                            {
                                if (!unit.CanTeleport || !otherPortal.CanTeleport) break;

                                var unitToTeleport = unitsForTeleport[0];
                                _serviceZone.SendPortalTeleport(unitToTeleport.Id, unit.Id, otherPortal.Id);
                                var telePos = otherPortal.GetMidpoint();
                                telePos = telePos with
                                {
                                    Y = telePos.Y - (unitMidpoint.Y - unitToTeleport.Transform.Position.Y)
                                };
                                _serviceZone.SendUnitManeuver(unitToTeleport.Id, new ManeuverTeleport
                                {
                                    Position = telePos
                                });

                                var blankLink = new PortalLink
                                {
                                    LinkedPortalUnitId = null
                                };
                                unit.JustTeleported = true;
                                unit.LastTeleport = DateTimeOffset.Now;
                                UnitUpdated(unit, new UnitUpdate
                                {
                                    PortalLink = blankLink
                                });
                                otherPortal.JustTeleported = true;
                                otherPortal.LastTeleport = DateTimeOffset.Now;
                                UnitUpdated(otherPortal, new UnitUpdate
                                {
                                    PortalLink = blankLink
                                });
                            }
                            else
                            {
                                var teleport2Range = new BoundingBoxEx(otherPortal.GetMidpoint(),
                                    new Vector3(0.5f, portalSize.y, 0.5f));
                            
                                var otherForTeleport = _unitOctree.GetColliding(teleport2Range).Where(u =>
                                    u.Id != otherPortal.Id && (portalData.UnitsFilter is not { } targeting ||
                                                               u.DoesEffectApply(targeting, otherPortal.Team))).ToList();

                                if (otherForTeleport.Count == 0)
                                {
                                    if (unit.JustTeleported)
                                    {
                                        UnitUpdated(unit, new UnitUpdate
                                        {
                                            PortalLink = unit.PortalLinked
                                        });
                                    }
                                    unit.JustTeleported = false;

                                    if (otherPortal.JustTeleported)
                                    {
                                        UnitUpdated(otherPortal, new UnitUpdate
                                        {
                                            PortalLink = otherPortal.PortalLinked
                                        });
                                    }
                                    otherPortal.JustTeleported = false;
                                }
                            }
                        }
                        break;
                    
                    case UnitDataPlayer:
                        if (unit is { RespawnTime: not null, IsDead: true, PlayerId: not null } &&
                            unit.RespawnTime < DateTimeOffset.Now &&
                            _zoneData.PlayerSpawnPoints.TryGetValue(unit.PlayerId.Value, out var spawn) &&
                            spawn is not null && _zoneData.SpawnPoints.TryGetValue(spawn.Value, out var spawnPoint) && 
                            spawnPoint.Lock is SpawnPointLockType.Free)
                        {
                            if (_mapSpawnPoints.TryGetValue(spawnPoint.Id, out var mapSpawnPoint))
                            {
                                var spawnPos = GetSpawnPosition(mapSpawnPoint.Position, 2);
                                var spawnRot = Direction2dHelper.Rotation(mapSpawnPoint.Direction);
                                if (unit.Respawn(spawnPos, spawnRot))
                                {
                                    _zoneData.UpdateSpawnTime(unit.PlayerId.Value, null);
                                }
                            }
                            else if (_playerSpawnPoints.TryGetValue(spawnPoint.Id, out var playerSpawnPoint))
                            {
                                var spawnMidpoint = playerSpawnPoint.GetMidpoint();
                                var spawnPos = spawnMidpoint with
                                {
                                    Y = spawnMidpoint.Y - (playerSpawnPoint.UnitCard?.Size?.y ?? 0) / 2.0f +
                                        0.08f
                                };

                                spawnPos = GetSpawnPosition(spawnPos, playerSpawnPoint.UnitCard?.SpawnPoint?.SideShift ?? 0);
                                if (unit.Respawn(spawnPos, Quaternion.Identity, playerSpawnPoint.Transform.Rotation))
                                {
                                    _zoneData.UpdateSpawnTime(unit.PlayerId.Value, null);
                                }
                            }
                        }
                        if (doBuffCheck)
                        {
                            if (unit.IsNewAbilityChargeReady && !isDisabled)
                                unit.AbilityChargeGained();
                            if (unit.IsTriggerTimeUp)
                            {
                                unit.AbilityUsed();
                                unit.AbilityTriggered = false;
                                unit.AbilityTriggerTimeEnd = null;
                                unit.RemoveTriggerEffects();
                            }
                        }
                        break;
                    
                    case UnitDataShower showerData when !unit.ShowerStarted:
                        unit.StartShower(rand => OnShower(unit, showerData, rand));
                        break;
                }

                if (!doBuffCheck) continue;
                
                unit.ApplyBuffEffects(BuffMultiplier);
                
                if (unit.WinningTeam == _winningTeam) continue;
                foreach (var contextEffect in unit.MatchContextEffects)
                {
                    OnMatchContextChanged(unit, unit.WinningTeam, _winningTeam, contextEffect);                
                }

                unit.WinningTeam = _winningTeam;
            }

            foreach (var (block, blockUpdater) in MapBinary.UnitsInsideBlock)
            {
                var applyUnits = blockUpdater.GetApplyIntervalTo();
                var blockPos = block.ToVector3();
                if (blockUpdater.Effect.IntervalEffect?.Effect is null) continue;
                foreach (var unit in applyUnits)
                {
                    var (max, min) = UnitSizeHelper.GetExactUnitBounds(unit);
                    var blockImpact = MapBinary.CreateImpactForBlock(block,
                        Vector3.Clamp(Vector3.Clamp(CoordsHelper.BlockBottom(block), min, max),
                            blockPos + UnitSizeHelper.ImprecisionVector,
                            blockPos + Vector3.One - UnitSizeHelper.ImprecisionVector));
                    var blockSource = new BlockSource(block, MapBinary[block].ToBlock(), blockImpact);
                    
                    if (blockUpdater.Effect.IntervalEffect.TargetUnit)
                    {
                        ApplyInstEffect(blockSource, [unit], blockUpdater.Effect.IntervalEffect.Effect, blockImpact,
                            damageBlock: blockUpdater.Effect.IntervalEffect.TargetSelf);
                    }
                    else if (blockUpdater.Effect.IntervalEffect.TargetSelf)
                    {
                        ApplyInstEffect(blockSource, [], blockUpdater.Effect.IntervalEffect.Effect, blockImpact,
                            damageBlock: true);
                    }
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

        try
        {
            EnqueueAction(UpdatePhase);
        } 
        catch (ObjectDisposedException)
        {
        }
    }

    private void OnBuild2TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_build2Timer == null) return;
        _build2Timer.Stop();
        _build2Timer.Dispose();
        _build2Timer = null;
        
        try
        {
            EnqueueAction(UpdatePhase);
        } 
        catch (ObjectDisposedException)
        {
        }
    }

    private void OnRespawnTimerIncreased(object? sender, ElapsedEventArgs e)
    {
        if (_respawnIncreaseTimer == null)
        {
            return;
        }
        
        _respawnIncreaseTimer.Stop();
        try
        {
            if (EnqueueAction(() => IncreaseSpawnTime(_respawnIncreaseTimer)))
            {
                _respawnIncreaseTimer.Start();
            }
            else
            {
                _respawnIncreaseTimer.Dispose();
                _respawnIncreaseTimer = null;
            }
        }
        catch (ObjectDisposedException)
        {
            if (_respawnIncreaseTimer != null)
            {
                _respawnIncreaseTimer.Dispose();
                _respawnIncreaseTimer = null;
            }
        }
        
    }
}