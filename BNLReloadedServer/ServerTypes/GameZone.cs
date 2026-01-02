using System.Numerics;
using System.Timers;
using System.Linq;
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
    private const float UnitSpawnYOffset = 0.08f;
    
    public CancellationTokenSource GameCanceler { get; } = new();
    
    private readonly ZoneData _zoneData;

    private MapBinary MapBinary => _zoneData.BlocksData;
    public ZoneInitData BeginningZoneInitData { get; }
    
    private readonly IServiceZone _serviceZone;
    private readonly IServiceZone _unbufferedZone;
    private readonly IBuffer _sendBuffer;
    private readonly ISender _sessionsSender;
    
    private readonly IGameInitiator _gameInitiator;
    
    private readonly ICollection<PlayerLobbyState> _playerLobbyInfo;
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
    private readonly DateTimeOffset?[] _lastSurrenderTime = new DateTimeOffset?[Enum.GetValues<TeamType>().Length];
    private TeamType _winningTeam = TeamType.Neutral;
    private readonly List<(uint, UnitInit, Func<IServiceZone?>)> _createOnStart = [];
    private readonly List<(uint, UnitUpdate, Func<IServiceZone?>)> _updateOnStart = [];
    
    private readonly Dictionary<ulong, ShotInfo> _shotInfo = new();
    private readonly HashSet<ulong> _keepShotAlive = [];
    private readonly HashSet<ulong> _checkForWater = [];
    private readonly List<Unit> _unitsToDrop = [];

    private DateTimeOffset? _attackStartTime;
    private DateTimeOffset? _matchEndTime;
    private float? _finalMatchDurationSeconds;
    
    private Task? _gameLoop;
    private Task? _tickChecker;
    private Task? _endMatchTask;

    private Timer? _build1Timer;
    private Timer? _build2Timer;

    private float _respawnTime;
    private int _increaseTimes;
    private Timer? _respawnIncreaseTimer;

    private int _supplyTimes;
    private Timer? _supplyTimer;

    private readonly UnitUpdater _defaultUnitUpdater;

    private uint _newUnitId = 1;
    private uint _newSpawnId = 1;

    private readonly string? _instanceId;

    private ulong _tickNumber;
    private ulong _lastTickNumber;

    private uint NewUnitId() => _newUnitId++;
    private uint NewSpawnId() => _newSpawnId++;

    public bool HasEnded => _zoneData.MatchEnded;

    public GameZone(IServiceZone serviceZone, IServiceZone unbufferedZone, IBuffer sendBuffer, ISender sessionsSender,
        MapData mapData, IGameInitiator gameInitiator, ICollection<PlayerLobbyState> players, Key? mapKey = null)
    {
        _serviceZone = serviceZone;
        _unbufferedZone = unbufferedZone;
        _sendBuffer = sendBuffer;
        _sessionsSender = sessionsSender;
        _gameInitiator = gameInitiator;
        _playerLobbyInfo = players;
        _instanceId = gameInitiator.GameInstanceId;
        
        foreach (var team in Enum.GetValues<TeamType>())
        {
            _objectiveConquest[(int)team] = new Queue<UnitLabel>();
        }

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
            LinkPortal,
            OnPull,
            UnitCreated,
            OnDisarmed,
            ChangeId,
            GetPlayerFromPlayerId,
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
            BlocksData = new MapBinary(mapData.Schema, mapData.BlocksData ?? [],
                mapData.Size, mapData.Properties?.PlanePosition ?? 0, new MapUpdater(OnCut, OnMined, OnDetached, EnqueueAction)),
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
        foreach (var unit in _units.Where(u => u.Value.PlayerId is null && !u.Value.IsDead))
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
                player.Value.ZoneService = savedService;
            }
            else if (!player.Value.IsDead)
            {
                zoneService.SendUnitCreate(player.Key, player.Value.GetInitData());
                zoneService.SendUnitUpdate(player.Key, player.Value.GetUpdateData());
            }
        }

        if (_playerUnits.Values.Any(player => player.PlayerId == playerId) || _gameInitiator.IsPlayerSpectator(playerId)) return;
        var playerUnit = CreatePlayerUnit(playerId, zoneService);
        if (playerUnit == null) return;
        playerUnit.ZoneService = savedService;

        if (_gameLoop is null)
        {
            _updateOnStart.Add((playerUnit.Id, playerUnit.GetUpdateData(), () => playerUnit.ZoneService));
        }
        else
        {
            zoneService.SendUnitUpdate(playerUnit.Id, playerUnit.GetUpdateData());
        }
    }

    private Unit? CreatePlayerUnit(uint playerId, IServiceZone creatorService)
    {
        var playerInfo = _playerLobbyInfo.FirstOrDefault(player => player.PlayerId == playerId);
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

    private void CreateSupplyUnit(Key supplyKey, Vector3 position)
    {
        if (_zoneData.MatchCard.SupplyLogic is not { } supplyLogic) return;
        var spawnPoint = position with { Y = position.Y + supplyLogic.SpawnHeight };
        var transform = ZoneTransformHelper.ToZoneTransform(spawnPoint, Quaternion.Identity);

        if (_gameInitiator.IsSuperSupplies() && supplyKey == CatalogueHelper.SupplyDrop)
        {
            supplyKey = CatalogueHelper.SuperSupplyDrop;
        }
        
        CatalogueFactory.CreateUnit(NewUnitId(), supplyKey, transform, TeamType.Neutral, null, _defaultUnitUpdater);
    }

    private Vector3 GetSpawnPosition(Vector3 spawnPoint, float spawnRadius)
    {
        if (spawnRadius < 1) return spawnPoint with { Y = spawnPoint.Y + UnitSpawnYOffset };
        
        var spawnBlocks = (int)float.Floor(spawnRadius);
        var blockedPositions = MapBinary.GetContainedInUnits(
            _unitOctree.GetColliding(new BoundingBoxEx(spawnPoint, new Vector3(spawnBlocks * 2, 2, spawnBlocks * 2)))
                .Where(u => u.UnitCard?.Labels?.Contains(UnitLabel.RespawnPoint) is not true).ToList());

        List<Vector3> validPositions = [];
        for (var x = spawnPoint.X - spawnBlocks; x <= spawnPoint.X + spawnBlocks; x++)
        {
            for (var z = spawnPoint.Z - spawnBlocks; z <= spawnPoint.Z + spawnBlocks; z++)
            {
                var pos = new Vector3(x, spawnPoint.Y + UnitSpawnYOffset, z);
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
        
        return spawnPoint + new Vector3(spawnX, UnitSpawnYOffset, spawnZ);
    }

    private SpawnPointLockType IsMapSpawnRequirementsMet(MapSpawnPoint spawnPoint)
    {
        if (spawnPoint.Label != SpawnPointLabel.Objective1) return SpawnPointLockType.Free;

        var checkTeam = spawnPoint.Team switch
        {
            TeamType.Neutral => TeamType.Team1,
            TeamType.Team1 => TeamType.Team2,
            TeamType.Team2 => TeamType.Team1,
            _ => TeamType.Neutral
        };
        
        if (_objectiveConquest[(int)checkTeam].TryPeek(out var currObj))
        {
            return currObj != UnitLabel.Line1 ? SpawnPointLockType.Free : SpawnPointLockType.ServerBlocked;
        }
            
        return SpawnPointLockType.ServerBlocked;
    }

    private SpawnPointLockType CheckSpawn(Unit unit, Vector3 spawnPoint)
    {
        if (unit.IsBuff(BuffType.Disabled) || unit.IsDead)
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

    private Vector3? GetSupplyPosition(SupplySequenceItem supply)
    {
        var dropPoints = _units.Values.ToList().Where(u => u.UnitCard?.Labels?.Contains(supply.DropPointLabel) ?? false)
            .Select(u => u.Transform.Position).ToArray();
        if (dropPoints.Length == 0)
        {
            return null;
        }
        
        var rand = new Random();
        var dropPoint = rand.GetItems(dropPoints, 1)[0];
        var offset = _zoneData.MatchCard.SupplyLogic?.RandomPosOffset;
        if (!(offset > 0)) return dropPoint;
        
        var spawnX = (rand.NextSingle() * 2 - 1) * offset.Value;
        var spawnZ = (rand.NextSingle() * 2 - 1) * offset.Value;

        dropPoint.X += spawnX;
        dropPoint.Z += spawnZ;

        return dropPoint;
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
        switch (_zoneData.MatchCard.Data?.Type)
        {
            case MatchType.Tutorial:
                var objectiveUnitCount = _zoneData.MapData.Units.Count(u =>
                    u.UnitKey.GetCard<CardUnit>()?.Labels?.Contains(UnitLabel.TutorialCheckpoint) is true);
                _zoneData.Objectives =
                [
                    new ZoneObjective
                    {
                        Counter = 0,
                        Id = 0,
                        RequiredCounter = objectiveUnitCount,
                        Team = TeamType.Team1
                    }
                ];
                break;
            case MatchType.TimeTrial:
                var objectives = _zoneData.GetTimeTrialCourse()?.MatchObjectives;
                if (objectives is not { Count: > 0 })
                {
                    break;
                }

                foreach (var objective in objectives)
                {
                    var unitCount = objective switch
                    {
                        MatchObjectiveCollectPickups matchObjectiveCollectPickups => _zoneData.MapData.Units.Count(u =>
                            matchObjectiveCollectPickups.PickupKey is not null
                            ? u.UnitKey == matchObjectiveCollectPickups.PickupKey
                            : matchObjectiveCollectPickups.PickupLabel is not null && u.UnitKey.GetCard<CardUnit>()
                                ?.Labels?.Contains(matchObjectiveCollectPickups.PickupLabel.Value) is true),
                        MatchObjectiveKillUnits matchObjectiveKillUnits => matchObjectiveKillUnits.Limit,
                        _ => 0
                    };

                    if (unitCount is 0 or null)
                    {
                        continue;
                    }
                    
                    _zoneData.Objectives.Add(new ZoneObjective
                    {
                        Counter = 0,
                        Id = objective.Id,
                        RequiredCounter = unitCount.Value,
                        Team = objective.Team
                    });
                }
                break;
        }
        
        if (_zoneData.MatchCard.Data?.Type is MatchType.TimeTrial) return;
        
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

    private ZoneUpdate GetInitialZoneUpdate() =>
        new()
        {
            Phase = _zoneData.Phase,
            PlayerInfo = _zoneData.PlayerInfo,
            SpawnPoints = _zoneData.SpawnPoints.Values.ToList(),
            ResourceCap = _zoneData.ResourceCap,
            Objectives = _zoneData.MatchCard.Data?.Type is MatchType.TimeTrial or MatchType.Tutorial
                ? _zoneData.Objectives
                : null
        };

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
                _attackStartTime = DateTimeOffset.Now;
                
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

                if (_zoneData.MatchCard.SupplyLogic?.Sequence is { Count: > 0 } supplySequence)
                { 
                    _zoneData.UpdateSupplyTime(supplySequence[0], GetSupplyPosition(supplySequence[0]));
                    _supplyTimer = new Timer(TimeSpan.FromSeconds(supplySequence[0].Seconds).TotalMilliseconds);
                    _supplyTimer.Elapsed += OnSupplyTimerElapsed;
                    _supplyTimer.AutoReset = false;
                    _supplyTimer.Start();
                }
                else if (_zoneData.MatchCard.SupplyLogic?.RepeatSequence is { Count: > 0 } repSequence)
                {
                    _zoneData.UpdateSupplyTime(repSequence[0], GetSupplyPosition(repSequence[0]));
                    _supplyTimer = new Timer(TimeSpan.FromSeconds(repSequence[0].Seconds).TotalMilliseconds); 
                    _supplyTimer.Elapsed += OnSupplyTimerElapsed;
                    _supplyTimer.AutoReset = false;
                    _supplyTimer.Start();
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

        if (currentPhase is ZonePhaseType.Waiting or ZonePhaseType.TutorialInit)
        {
            for (var index = 0; index < _createOnStart.Count; index++)
            {
                var (unitId, unitInit, serviceZone) = _createOnStart[index];
                serviceZone()?.SendUnitCreate(unitId, unitInit);
            }
            
            for (var index = 0; index < _updateOnStart.Count; index++)
            {
                var (unitId, unitUpdate, serviceZone) = _updateOnStart[index];
                serviceZone()?.SendUnitUpdate(unitId, unitUpdate);
            }

            _createOnStart.Clear();
            _updateOnStart.Clear();
        }
        
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
                if (MapBinary.ContainsBlock(attachedBlockPos) && MapBinary[attachedBlockPos].IsSolid)
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
        _tickChecker = RunTickCheck();
        _gameInitiator.SetBackfillReady(_zoneData.GameModeCard.Ranking is GameRankingType.Friendly);
    }

    private void IncreaseSpawnTime(Timer? timer = null)
    {
        var respTimes = _zoneData.MatchCard.RespawnLogic?.IncrementSequence;
        var repRespTimes = _zoneData.MatchCard.RespawnLogic?.IncrementRepeatSequence;
        if (respTimes is { Count: > 0 } && _increaseTimes < respTimes.Count)
        {
            var resp = respTimes[_increaseTimes];
            _respawnTime += resp.RespawnTimeIncSeconds;
            _increaseTimes++;
            timer?.Interval = TimeSpan.FromSeconds(resp.MatchSeconds).TotalMilliseconds;
        }
        else if (repRespTimes is { Count: > 0 } && (_zoneData.MatchCard.RespawnLogic?.IncrementRepeatLimit is null ||
                                                    _increaseTimes - (respTimes?.Count ?? 0) <
                                                    _zoneData.MatchCard.RespawnLogic.IncrementRepeatLimit))
        {
            var incTimes = _increaseTimes - (respTimes?.Count ?? 0);
            var resp = repRespTimes[incTimes % repRespTimes.Count];
            _respawnTime += resp.RespawnTimeIncSeconds;
            _increaseTimes++;
            timer?.Interval = TimeSpan.FromSeconds(resp.MatchSeconds).TotalMilliseconds;
        }
    }

    public void JoinedInProgress(uint playerId, IServiceZone zoneService)
    {
        var lobbyInfo = _playerLobbyInfo.FirstOrDefault(x => x.PlayerId == playerId);
        if (lobbyInfo == null)
        {
            zoneService.SendUpdateZone(new ZoneUpdate
            {
                Statistics = new MatchStats
                {
                    PlayerStats = _zoneData.PlayerStats,
                    Team1Stats = _zoneData.GetTeamScores(TeamType.Team1),
                    Team2Stats = _zoneData.GetTeamScores(TeamType.Team2)
                },
                PlayerSpawnPoints = _zoneData.PlayerSpawnPoints,
                Phase = _zoneData.Phase,
                PlayerInfo = _zoneData.PlayerInfo,
                Objectives = _zoneData.MatchCard.Data?.Type is MatchType.TimeTrial or MatchType.Tutorial
                    ? _zoneData.Objectives
                    : null,
                SpawnPoints = _zoneData.SpawnPoints.Values.ToList()
            });
            return;
        }
        
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

        if (!_zoneData.PlayerInfo.ContainsKey(playerId))
        {
            _zoneData.PlayerInfo.Add(playerId, new ZonePlayerInfo
            {
                SquadId = lobbyInfo.SquadId,
                LookingForFriends = lobbyInfo.LookingForFriends,
                Nickname = lobbyInfo.Nickname,
                SteamId = lobbyInfo.SteamId
            });
        }

        _mapSpawnPoints.TryGetValue(_defaultSpawnId[(int)lobbyInfo.Team], out var spawn);
        _zoneData.PlayerSpawnPoints[playerId] = spawn != null ? _defaultSpawnId[(int)lobbyInfo.Team] : null;

        var matchZoneUpdate = new ZoneUpdate
        {
            Statistics = new MatchStats
            {
                PlayerStats = _zoneData.PlayerStats,
                Team1Stats = _zoneData.GetTeamScores(TeamType.Team1),
                Team2Stats = _zoneData.GetTeamScores(TeamType.Team2)
            },
            PlayerSpawnPoints = _zoneData.PlayerSpawnPoints,
            PlayerInfo = _zoneData.PlayerInfo,
            Objectives = _zoneData.MatchCard.Data?.Type is MatchType.TimeTrial or MatchType.Tutorial
                ? _zoneData.Objectives
                : null
        };

        _serviceZone.SendUpdateZone(matchZoneUpdate);
        zoneService.SendUpdateZone(new ZoneUpdate
        {
            Phase = _zoneData.Phase,
            SpawnPoints = _zoneData.SpawnPoints.Values.ToList()
        });
        
        var playerUnit = _playerUnits.Values.FirstOrDefault(p => p.PlayerId == playerId);
        if (playerUnit is not null)
        {
            if (!playerUnit.IsActive)
            {
                playerUnit.RespawnTime = DateTimeOffset.Now.AddSeconds(-1);
                playerUnit.IsActive = true;
            }
            else
            {
                MovementActive(playerUnit);
            }
        }
    }

    public void PlayerDisconnected(uint playerId)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var unitId) ||
            !_playerUnits.TryGetValue(unitId, out var player))
        {
            return;
        }

        var impact = new ImpactData
        {
            Crit = false,
            InsidePoint = player.GetMidpoint(),
            ShotPos = player.GetMidpoint(),
            Normal = Vector3s.Zero
        };
        
        player.IsActive = false;
        player.Killed(impact);
        
        _serviceZone.SendKickPlayer(playerId, KickReason.MatchQuit);
        _zoneData.UpdatePlayerSelectedSpawn(playerId, _defaultSpawnId[(int) player.Team]);
    }

    public bool PlayerLeft(uint playerId, KickReason reason)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var unitId) ||
            !_playerUnits.TryGetValue(unitId, out var player))
        {
            if (!_zoneData.PlayerInfo.ContainsKey(playerId) && !_zoneData.PlayerStats.ContainsKey(playerId))
                return false;
            
            _playerIdToUnitId.Remove(playerId);
            _zoneData.PlayerSpawnPoints.Remove(playerId);
            _zoneData.RespawnInfo.Remove(playerId);
            if (HasEnded) return true;
            
            _zoneData.PlayerStats.Remove(playerId);
            _zoneData.PlayerInfo.Remove(playerId);
            return true;
        }

        var impact = new ImpactData
        {
            Crit = false,
            InsidePoint = player.GetMidpoint(),
            ShotPos = player.GetMidpoint(),
            Normal = Vector3s.Zero
        };
        
        player.IsActive = false;
        player.Killed(impact);
        
        if (_gameInitiator is MatchmakerInitiator initiator)
        {
            initiator.RemovePlayer(playerId);
        }
        
        _serviceZone.SendKickPlayer(playerId, reason);

        if (!_zoneData.MatchEnded && !_gameInitiator.IsMapEditor() && _zoneData.GameModeCard.ExitMatchBehaviour is ExitMatchBehaviourType.Demerit or ExitMatchBehaviourType.Restricted)
        {
            var winners = _playerUnits.Values.Where(p => p.Team != player.Team).Select(p => p.PlayerId).OfType<uint>()
                .ToList();
            var losers = _playerUnits.Values.Where(p => p.Team == player.Team).Select(p => p.PlayerId).OfType<uint>()
                .ToList();
            var exclude = _playerUnits.Values.Where(p => p.PlayerId != playerId).Select(p => p.PlayerId).OfType<uint>()
                .ToHashSet();
            Databases.PlayerDatabase.UpdateRatings(winners, losers, exclude);

            if (player.Stats != null)
            {
            var statInfo = _zoneData.MatchCard.Stats?.Stats?.ToDictionary(k => k.Key,
                v => (int)v.Value.Sum(score => player.Stats.GetValueOrDefault(score.Key) * score.Value));
            var totalInfo = _zoneData.MatchCard.Stats?.Total;

            if (player.PlayerId is { } kickedPlayerId && statInfo is not null &&
                _zoneData.PlayerStats.TryGetValue(kickedPlayerId, out var aggregatedStats))
            {
                statInfo[PlayerMatchStatType.Destroyed] = aggregatedStats.BlocksDestroyed;
            }
                Databases.PlayerDatabase.UpdateMatchStats(new EndMatchResults
                {
                    PlayerId = playerId,
                    GameInstanceId = _instanceId ?? string.Empty,
                    MatchEndTime = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    MapKey = _zoneData.MapKey ?? Key.None,
                    GameModeKey = _zoneData.GameModeKey,
                    MatchData = new EndMatchData
                    {
                        MatchSeconds = (float)(_attackStartTime.HasValue ? DateTimeOffset.Now - _attackStartTime.Value : TimeSpan.Zero).TotalSeconds,
                        PlayersData = [new EndMatchPlayerData
                            {
                                PlayerId = playerId,
                                SquadId = null,
                                Backfiller = _gameInitiator.IsPlayerBackfill(playerId),
                                Noob = false,
                                Stats = new EndMatchPlayerStats
                                {
                                    Stats = statInfo,
                                    Total = (int)(totalInfo?.Sum(score => statInfo?[score.Key] * score.Value) ?? 0)
                                },
                                MedalPositive = default,
                                MedalNegative = default
                            }
                        ],
                        IsWinner = false,
                        IsBackfiller = _gameInitiator.IsPlayerBackfill(playerId),
                        IsAfk = true,
                        HeroKey = player.Key,
                        SkinKey = player.SkinKey
                    }
                });
            }
        }
        
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
            DoBlockUpdate(blkUpdates);
        }

        foreach (var proj in _keepShotAlive.ToList())
        {
            if (_shotInfo.TryGetValue(proj, out var shotInfo) && shotInfo.Caster.OwnerPlayerId == playerId)
            {
                ReceivedProjDropRequest(proj);
            }
        }
        
        _playerUnits.Remove(unitId);
        RemoveUnit(unitId);
        _playerIdToUnitId.Remove(playerId);
        _zoneData.PlayerSpawnPoints.Remove(playerId);
        _zoneData.RespawnInfo.Remove(playerId);
        if (!HasEnded)
        {
            _zoneData.PlayerStats.Remove(playerId);
            _zoneData.PlayerInfo.Remove(playerId);
        }
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

        return true;
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

    private void DoBlockUpdate(Dictionary<Vector3s, BlockUpdate> updates)
    {
        BeginningZoneInitData.Updates ??= new Dictionary<Vector3s, BlockUpdate>();
        foreach (var (pos, val) in updates)
        {
            var newVal = val;
            if (val.Id == 0 && val.Vdata != 0)
            {
                newVal = new BlockUpdate
                {
                    Damage = val.Damage,
                    Id = val.Id,
                    Vdata = 0,
                    Ldata = val.Ldata
                };
            }
            BeginningZoneInitData.Updates[pos] = newVal;
        }
        
        _unbufferedZone.SendBlockUpdates(updates);
    }

    private static void MovementActive(Unit unit)
    {
        unit.LastMoveTime = DateTimeOffset.Now;
        unit.WasAfkWarned = false;
        unit.UpdateData(new UnitUpdate { MovementActive = true });
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

    private bool CheckIfMatchOver(TeamType targetTeam, Unit? killer) =>
        _zoneData.MatchCard.Data?.Type switch
        {
            MatchType.ShieldRush2 or MatchType.ShieldCapture => _objectiveConquest[(int)targetTeam].Count == 0,
            MatchType.Tutorial or MatchType.TimeTrial => _zoneData.Objectives
                .Where(obj => obj.Team == (killer?.Team ?? TeamType.Neutral))
                .All(obj => obj.Counter >= obj.RequiredCounter),
            _ => false
        };

    private async Task BeginEndOfGame(TeamType winner, bool doWait = true)
    {
        EnqueueAction(() =>
        {
            _gameInitiator.SetBackfillReady(false);
            _zoneData.EndMatch(winner);
            if (_attackStartTime.HasValue && _matchEndTime is null)
            {
                _matchEndTime = DateTimeOffset.Now;
                _finalMatchDurationSeconds =
                    (float)(_matchEndTime.Value - _attackStartTime.Value).TotalSeconds;
            }
        });

        if (doWait)
        {
            await Task.Delay(TimeSpan.FromSeconds(_zoneData.MatchCard.Data switch
            {
                MatchDataShieldCapture matchDataShieldCapture => matchDataShieldCapture.EndMatchDelay,
                MatchDataShieldRush2 matchDataShieldRush2 => matchDataShieldRush2.EndMatchDelay,
                _ => 0
            }));
        }
        
        EnqueueAction(() => EndGame(winner));
    }

    private void EndGame(TeamType winner)
    {
        _unbufferedZone.SendEndMatch(winner);
        var matchStats = new List<EndMatchPlayerData>();
        var gameEnd = DateTimeOffset.Now;
        var gameLength = _finalMatchDurationSeconds.HasValue
            ? TimeSpan.FromSeconds(_finalMatchDurationSeconds.Value)
            : _attackStartTime.HasValue
                ? gameEnd - _attackStartTime.Value
                : TimeSpan.Zero;

        var cardGlobalLogic = CatalogueHelper.GlobalLogic;
        var positiveMedals = cardGlobalLogic.Medals?.PositiveMedals
            ?.Select(m => m.GetCard<CardMatchMedal>()).OfType<CardMatchMedal>().ToList() ?? [];
        var negativeMedals = cardGlobalLogic.Medals?.NegativeMedals
            ?.Select(m => m.GetCard<CardMatchMedal>()).OfType<CardMatchMedal>().ToList() ?? [];
        
        // Grab match stats
        foreach (var player in _playerUnits.Values)
        {
            if (player.Stats is null || player.PlayerId is null)
                continue;
            
            player.UpdateStatsFromTimers();
            if (player.Team == winner)
            {
                player.UpdateStatsFromWin();
            }
            
            var positiveMedal = positiveMedals.MaxBy(medal =>
                medal.ServerCounters?.Sum(score => player.Stats.GetValueOrDefault(score.Key) * score.Value));
            
            var negativeMedal = negativeMedals.MaxBy(medal =>
                medal.ServerCounters?.Sum(score => player.Stats.GetValueOrDefault(score.Key) * score.Value));
            
            var zoneDataMatchCard = _zoneData.MatchCard;
            var statInfo = zoneDataMatchCard.Stats?.Stats?.ToDictionary(k => k.Key,
                v => (int)v.Value.Sum(score => player.Stats.GetValueOrDefault(score.Key) * score.Value));
            var totalInfo = zoneDataMatchCard.Stats?.Total;

            if (player.PlayerId is { } playerId && statInfo is not null &&
                _zoneData.PlayerStats.TryGetValue(playerId, out var aggregatedStats))
            {
                statInfo[PlayerMatchStatType.Destroyed] = aggregatedStats.BlocksDestroyed;
            }
            
            var playerInfo = _playerLobbyInfo.FirstOrDefault(u => u.PlayerId == player.PlayerId);
            matchStats.Add(new EndMatchPlayerData
            {
                PlayerId = player.PlayerId.Value,
                SquadId = playerInfo?.SquadId,
                Backfiller = _gameInitiator.IsPlayerBackfill(player.PlayerId.Value),
                Noob = false,
                Stats = new EndMatchPlayerStats
                {
                    Stats = statInfo,
                    Total = (int)(totalInfo?.Sum(score => statInfo?[score.Key] * score.Value) ?? 0)
                },
                MedalPositive = positiveMedal?.Key ?? Key.None,
                MedalNegative = negativeMedal?.Key ?? Key.None
            });
        }
        
        // Send match stats
        var zoneDataGameModeCard = _zoneData.GameModeCard;
        var winners = new List<uint>();
        var losers = new List<uint>();
        var exclude = new HashSet<uint>();
        var inactivePlayers = new List<uint>();
        foreach (var player in _playerUnits.Values)
        {
            var wasInactive = !player.IsActive;
            player.IsActive = false;
            if (player.PlayerId is null)
                continue;

            var endMatchData = new EndMatchData
            {
                MatchSeconds = (float)gameLength.TotalSeconds,
                PlayersData = matchStats,
                IsWinner = player.Team == winner,
                IsBackfiller = _gameInitiator.IsPlayerBackfill(player.PlayerId.Value),
                IsAfk = wasInactive,
                HeroKey = player.Key,
                SkinKey = player.SkinKey
            };

            if (wasInactive)
            {
                inactivePlayers.Add(player.PlayerId.Value);
            }

            if (!endMatchData.IsAfk)
            {
                if (endMatchData.IsWinner)
                {
                    winners.Add(player.PlayerId.Value);
                }
                else
                {
                    losers.Add(player.PlayerId.Value);
                }

                if (endMatchData.IsBackfiller)
                {
                    exclude.Add(player.PlayerId.Value);
                }
            }
            
            var profileData = Databases.PlayerDatabase.GetPlayerProfile(player.PlayerId.Value);
            
            endMatchData.OldPlayerXp = profileData.Progression?.PlayerProgress;
            endMatchData.OldHeroXp = profileData.Progression?.HeroesProgress?.GetValueOrDefault(player.Key);

            var xpAmount = zoneDataGameModeCard.XpLogic is not null
                ? float.Clamp(zoneDataGameModeCard.XpLogic.XpPerMinute * (float)gameLength.TotalMinutes,
                    zoneDataGameModeCard.XpLogic.MinXpCap, zoneDataGameModeCard.XpLogic.MaxXpCap)
                : 0;

            var applicableBonuses = new Dictionary<MatchRewardBonusType, float>();
            if (zoneDataGameModeCard.RewardLogic?.Bonuses is not null)
            {
                foreach (var (bonus, amount) in zoneDataGameModeCard.RewardLogic.Bonuses)
                {
                    switch (bonus)
                    {
                        case MatchRewardBonusType.Victory when endMatchData.IsWinner:
                            applicableBonuses[MatchRewardBonusType.Victory] = amount;
                            xpAmount *= 1 + amount;
                            break;
                        case MatchRewardBonusType.Backfilling when endMatchData.IsBackfiller:
                            applicableBonuses[MatchRewardBonusType.Backfilling] = amount;
                            xpAmount *= 1 + amount;
                            break;
                        case MatchRewardBonusType.Shorthand when _playerUnits.Values.Count(p => p.Team == player.Team) <
                                                                 _playerUnits.Values.Count(p => p.Team != player.Team):
                            applicableBonuses[MatchRewardBonusType.Shorthand] = amount;
                            xpAmount *= 1 + amount;
                            break;
                    }
                }
            }

            endMatchData.RewardXp = _gameInitiator.IsMapEditor() ? 0 : xpAmount;
            endMatchData.NewHeroXp = endMatchData.OldHeroXp;
            if (endMatchData.OldHeroXp is not null && xpAmount > 0)
            {
                endMatchData.NewHeroXp = CatalogueHelper.LeveLUpHero(endMatchData.OldHeroXp, xpAmount);
            }

            var currencyAmountVirtual = 0f;
            var currencyAmountReal = 0f;
            if (!_gameInitiator.IsMapEditor() && (zoneDataGameModeCard.CurrencyLogic?.CurrencyPerMinute?.TryGetValue(CurrencyType.Virtual,
                    out var currPerMinVir) ?? false))
            {
                currencyAmountVirtual = currPerMinVir * (float)gameLength.TotalMinutes;
                if
                    (zoneDataGameModeCard.CurrencyLogic.MinCap?.TryGetValue(CurrencyType.Virtual,
                         out var currMinCapVir) ?? false)
                {
                    currencyAmountVirtual = MathF.Min(currencyAmountVirtual, currMinCapVir);
                }

                if (zoneDataGameModeCard.CurrencyLogic.MaxCap?.TryGetValue(CurrencyType.Virtual,
                        out var currMaxCapVir) ?? false)
                {
                    currencyAmountVirtual = MathF.Min(currencyAmountVirtual, currMaxCapVir);
                }
            }
            
            if (!_gameInitiator.IsMapEditor() && (zoneDataGameModeCard.CurrencyLogic?.CurrencyPerMinute?.TryGetValue(CurrencyType.Real,
                    out var currPerMinReal) ?? false))
            {
                currencyAmountReal = currPerMinReal * (float)gameLength.TotalMinutes;
                if
                    (zoneDataGameModeCard.CurrencyLogic.MinCap?.TryGetValue(CurrencyType.Real,
                         out var currMinCapReal) ?? false)
                {
                    currencyAmountReal = MathF.Min(currencyAmountReal, currMinCapReal);
                }

                if (zoneDataGameModeCard.CurrencyLogic.MaxCap?.TryGetValue(CurrencyType.Virtual,
                        out var currMaxCapReal) ?? false)
                {
                    currencyAmountReal = MathF.Min(currencyAmountReal, currMaxCapReal);
                }
            }
            
            endMatchData.OldCurrency = Databases.PlayerDatabase.GetCurrency(player.PlayerId.Value);
            endMatchData.RewardCurrency = new Dictionary<CurrencyType, float>
            {
                { CurrencyType.Virtual, currencyAmountVirtual },
                { CurrencyType.Real, currencyAmountReal }
            };
            endMatchData.RewardBonuses = applicableBonuses;
            endMatchData.ChallengesData = [];

            if (!_gameInitiator.IsMapEditor() && zoneDataGameModeCard.Ranking is GameRankingType.Friendly or GameRankingType.Ranked)
            {
                Databases.PlayerDatabase.UpdateMatchStats(new EndMatchResults
                {
                    PlayerId = player.PlayerId.Value,
                    GameInstanceId = _instanceId ?? string.Empty,
                    MatchEndTime = (ulong)gameEnd.ToUnixTimeMilliseconds(),
                    MapKey = _zoneData.MapKey ?? Key.None,
                    GameModeKey = _zoneData.GameModeKey,
                    MatchData = endMatchData
                });
            }

            player.ZoneService?.SendEndMatchResult(endMatchData);
        }

        if (!_gameInitiator.IsMapEditor() && zoneDataGameModeCard.Ranking is GameRankingType.Friendly or GameRankingType.Ranked)
        {
            Databases.PlayerDatabase.UpdateRatings(winners, losers, exclude);
        }
        
        var unitsToClean = _units.Values.Where(u => u.UnitCard?.Labels?.Contains(UnitLabel.DestroyOnMatchEnd) is true).ToList();

        foreach (var unit in unitsToClean)
        {
            unit.Killed(unit.CreateBlankImpactData());
        }
        
        _gameInitiator.ClearInstance(_instanceId);
        
        foreach (var player in inactivePlayers)
        {
            Databases.RegionServerDatabase.GetGameInstance(player)?.PlayerLeftInstance(player, KickReason.MatchInactivity);
        }
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
                unit.AddEffects(effects.Select(eff => new ConstEffectInfo(eff)), blockSource.Team, blockSource);
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
        try
        {
            var token = GameCanceler.Token;
            EnqueueAction(() =>
            {
                foreach (var mapSpawnPoint in
                         _mapSpawnPoints.Where(s => s.Value.Label is SpawnPointLabel.Objective1))
                {
                    _zoneData.UpdateSpawn(mapSpawnPoint.Key, IsMapSpawnRequirementsMet(mapSpawnPoint.Value));
                }
            });
            while (await tickTimer.WaitForNextTickAsync(token))
            {
                EnqueueAction(OnTick(_tickNumber++));
            }
        }
        finally
        {
            tickTimer.Dispose();
        }
    }

    private async Task RunTickCheck()
    {
        var tickTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var token = GameCanceler.Token;
        while (await tickTimer.WaitForNextTickAsync(token))
        {
            var ticksLastSecond = _tickNumber - _lastTickNumber;
            _lastTickNumber = _tickNumber;
            if (ticksLastSecond < TicksPerSecond - 1)
            {
                Console.WriteLine($"Low TPS: {ticksLastSecond}");
            }
        }
        
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
                            ApplyInstEffect(unitSource, exiting, aura.LeaveEffect, unitSource.Impact);
                        }

                        if (aura.ConstantEffects != null)
                        {
                            exiting.ForEach(u =>
                                u.RemoveEffects(aura.ConstantEffects.Select(e => new ConstEffectInfo(e)),
                                    unit.Team, unitSource));
                        }
                    }

                    if (entering.Count > 0)
                    {
                        if (aura.EnterEffect != null)
                        {
                            ApplyInstEffect(unitSource, entering, aura.EnterEffect, unitSource.Impact);
                        }

                        if (aura.ConstantEffects != null)
                        {
                            entering.ForEach(u =>
                                u.AddEffects(aura.ConstantEffects.Select(e => new ConstEffectInfo(e)),
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
                    if (nearbyBlock is not null)
                    {
                        var imp = MapBinary.CreateImpactForBlock(nearbyBlock.Value, unit.GetMidpoint());
                        unit.AddEffects(nearby.Effects.Select(e => new ConstEffectInfo(e)), unit.Team,
                            new BlockSource(nearbyBlock.Value, MapBinary[nearbyBlock.Value].ToBlock(), imp));
                    }
                    else
                    {
                        unit.RemoveEffects(nearby.Effects.Select(e => new ConstEffectInfo(e)), unit.Team, null,
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

                        unit.CloudEffect.NearbyUnits = currColliders;
                        foreach (var exit in prevColliders.Except(currColliders))
                        {
                            exit.RemoveEffects(
                                unitDataCloud.InsideEffects.Select(e => new ConstEffectInfo(e)), unit.Team,
                                unitSource);
                        }
                        
                        foreach (var enter in currColliders.Except(prevColliders))
                        {
                            enter.AddEffects(unitDataCloud.InsideEffects.Select(e => new ConstEffectInfo(e)),
                                unit.Team, unitSource);
                        }
                        break;
                    
                    case UnitDataDamageCapture unitDataDamageCapture when unit.DamageCaptureEffect is not null:
                        var nearby = _unitOctree.GetColliding(unit.DamageCaptureEffect.Shape);
                        var prevBaddies = unit.DamageCaptureEffect.NearbyUnits;
                        var nearbyBaddies = nearby.Where(u =>
                            (u.UnitCard?.Labels?.Contains(unitDataDamageCapture.CapturerLabel) ?? false) &&
                            u.Team != unit.Team).ToArray();

                        var doUpdate = false;
                        unit.DamageCaptureEffect.NearbyUnits = nearbyBaddies;
                        if (unitDataDamageCapture.ZoneEffects is not null)
                        {
                            foreach (var baddie in prevBaddies.Except(nearbyBaddies))
                            {
                                doUpdate = true;
                                baddie.RemoveEffects(
                                    unitDataDamageCapture.ZoneEffects.Select(e => new ConstEffectInfo(e)),
                                    unit.Team, unitSource);
                            }
                            foreach (var baddie in nearbyBaddies.Except(prevBaddies))
                            {
                                doUpdate = true;
                                baddie.AddEffects(
                                    unitDataDamageCapture.ZoneEffects.Select(e => new ConstEffectInfo(e)),
                                    unit.Team, unitSource);
                            }
                        }

                        if (doUpdate)
                        {
                            unit.UpdateData(new UnitUpdate
                            {
                                DamageCapturers = nearbyBaddies.Select(u => u.Id).ToList()
                            });
                        }

                        if (doDmgCaptureCheck)
                        {
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
                        var nearbyEnemies = nearbyUnits.Where(u => u.PlayerId != null && u.Team != unit.Team);
                        if (nearbyEnemies.Any())
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
                                u.Id != unit.Id && (portalData.UnitsFilter is not { } targeting || u.DoesEffectApply(targeting, unit.Team)));

                            if (unitsForTeleport.FirstOrDefault() is {} unitToTeleport)
                            {
                                if (!unit.CanTeleport || !otherPortal.CanTeleport) break;
                                
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
                                                               u.DoesEffectApply(targeting, otherPortal.Team)));

                                if (!otherForTeleport.Any())
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
                        if (unit is { RespawnTime: not null, IsDead: true, PlayerId: not null, IsActive: true, IsDropped: true } &&
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
                                    unit.SpawnProtectionTime =
                                        DateTimeOffset.Now.AddSeconds(_zoneData.MatchCard.RespawnLogic
                                            ?.SpawnProtectionSeconds ?? 0);
                                    _zoneData.UpdateSpawnTime(unit.PlayerId.Value, null);
                                }
                            }
                            else if (_playerSpawnPoints.TryGetValue(spawnPoint.Id, out var playerSpawnPoint))
                            {
                                var spawnMidpoint = playerSpawnPoint.GetMidpoint();
                                var spawnPos = spawnMidpoint with
                                {
                                    Y = spawnMidpoint.Y - (playerSpawnPoint.UnitCard?.Size?.y ?? 0) / 2.0f
                                };

                                spawnPos = GetSpawnPosition(spawnPos, playerSpawnPoint.UnitCard?.SpawnPoint?.SideShift ?? 0);
                                if (unit.Respawn(spawnPos, Quaternion.Identity, playerSpawnPoint.Transform.Rotation))
                                {
                                    _zoneData.UpdateSpawnTime(unit.PlayerId.Value, null);
                                }
                            }
                        }
                        
                        if (unit.DoRecall)
                        {
                            unit.EndRecall();
                            if (_mapSpawnPoints.TryGetValue(_defaultSpawnId[(int)unit.Team], out var mapSpawnPoint))
                            {
                                var spawnPos = GetSpawnPosition(mapSpawnPoint.Position, 2);
                                _serviceZone.SendUnitManeuver(unit.Id, new ManeuverTeleport
                                {
                                    Position = spawnPos
                                });
                                _serviceZone.SendDoRecall(unit.Id);
                            }
                        }
                        
                        if (unit.IsNewAbilityChargeReady && !isDisabled)
                            unit.AbilityChargeGained();
                        if (unit.IsTriggerTimeUp)
                        {
                            unit.AbilityUsed();
                            unit.AbilityTriggered = false;
                            unit.AbilityTriggerTimeEnd = null;
                            unit.RemoveTriggerEffects();
                        }

                        if (unit.LastMoveTime.HasValue && (DateTimeOffset.Now - unit.LastMoveTime.Value).TotalSeconds >
                            _zoneData.GameModeCard.AntiAfk?.AfkWarningSeconds && unit.PlayerId is not null &&
                            _instanceId is not null && !HasEnded)
                        {
                            if (!unit.WasAfkWarned)
                            {
                                EnqueueAction(() => Databases.RegionServerDatabase.SendAfkWarning(unit.PlayerId.Value, _instanceId));
                                unit.WasAfkWarned = true;
                            }

                            if ((DateTimeOffset.Now - unit.LastMoveTime.Value).TotalSeconds >
                                _zoneData.GameModeCard.AntiAfk?.AfkPunishSeconds)
                            {
                                EnqueueAction(() => Databases.RegionServerDatabase.KickForAfk(unit.PlayerId.Value, _instanceId));
                                unit.LastMoveTime = null;
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

            foreach (var (_, idx) in
                     _zoneData.SurrenderEndTime.Select((s, idx) => (s, idx))
                         .Where(time => time.s.HasValue && time.s < DateTimeOffset.Now).ToList())
            {
                _serviceZone.SendSurrenderEnd((TeamType)idx, false);
                _zoneData.IsSurrenderRequest[idx] = false;
                _zoneData.SurrenderEndTime[idx] = null;
                _lastSurrenderTime[idx] = DateTimeOffset.Now;
            }
            
            if (_endMatchTask?.Status is TaskStatus.Created)
            {
                _endMatchTask.Start();
            }
            
            FlushBuffer();

            _unitsToDrop.ForEach(DropUnit);
            _unitsToDrop.Clear();
        };

    private void FlushBuffer() => _sendBuffer.UseBuffer(_sessionsSender.Send);

    private void OnBuild1TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_build1Timer == null) return;
        _build1Timer.Stop();
        _build1Timer.Dispose();
        _build1Timer = null;

        try
        {
            if (_zoneData.Phase.PhaseType is ZonePhaseType.Build)
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
            if (_zoneData.Phase.PhaseType is ZonePhaseType.Build2)
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
            if (EnqueueAction(() =>
                {
                    IncreaseSpawnTime(_respawnIncreaseTimer);
                    _respawnIncreaseTimer.Start();
                }))
            {
                return;
            }
                
            _respawnIncreaseTimer.Dispose();
            _respawnIncreaseTimer = null;
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

    private void OnSupplyTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_supplyTimer == null || _zoneData.MatchCard.SupplyLogic is null)
        {
            return;
        }
        
        _supplyTimer.Stop();
        try
        {
            var supplyDrop = _zoneData.SupplyInfo.NextSupplyDrop;
            var position = _zoneData.SupplyInfo.Position;
            if (supplyDrop is null || position is null ||
                EnqueueAction(() => CreateSupplyUnit(supplyDrop.Value, position.Value)))
            {
                _supplyTimes++;
                if (_zoneData.MatchCard.SupplyLogic.Sequence is { Count: > 0 } sequence &&
                    _supplyTimes < sequence.Count)
                {
                    _zoneData.UpdateSupplyTime(sequence[_supplyTimes], GetSupplyPosition(sequence[_supplyTimes]));
                    _supplyTimer.Interval = TimeSpan.FromSeconds(sequence[_supplyTimes].Seconds).TotalMilliseconds;
                    _supplyTimer.Start();
                }
                else if (_zoneData.MatchCard.SupplyLogic.RepeatSequence is { Count: > 0 } repeatSequence)
                {
                    var repIndex = (_supplyTimes - (_zoneData.MatchCard.SupplyLogic.Sequence?.Count ?? 0)) % repeatSequence.Count;
                    _zoneData.UpdateSupplyTime(repeatSequence[repIndex], GetSupplyPosition(repeatSequence[repIndex]));
                    _supplyTimer.Interval = TimeSpan.FromSeconds(repeatSequence[repIndex].Seconds).TotalMilliseconds;
                    _supplyTimer.Start();
                }
            }
            else
            {
                _supplyTimer.Dispose();
                _supplyTimer = null;
            }
        }
        catch (ObjectDisposedException)
        {
            if (_supplyTimer != null)
            {
                _supplyTimer.Dispose();
                _supplyTimer = null;
            }
        }
    }

    public (Dictionary<uint, MatchPlayerStats> stats, DateTimeOffset? attackStartTime, float? finalDurationSeconds)
        GetStatusSnapshot()
    {
        var stats = _zoneData.PlayerStats.ToDictionary(
            player => player.Key,
            player => new MatchPlayerStats
            {
                Team = player.Value.Team,
                Kills = player.Value.Kills,
                Deaths = player.Value.Deaths,
                Assists = player.Value.Assists,
                BlocksBuilt = player.Value.BlocksBuilt,
                BlocksDestroyed = player.Value.BlocksDestroyed,
                ResourcesEarned = player.Value.ResourcesEarned
            });

        return (stats, _attackStartTime, _finalMatchDurationSeconds);
    }
}