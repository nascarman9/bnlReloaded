using System.Numerics;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public record ZoneUpdater(Action<ZoneUpdate> OnZoneUpdate);

public class ZoneData(ZoneUpdater updater)
{
    public readonly Dictionary<TeamType, MatchTeamStats> TeamStats = new();
    public readonly Dictionary<uint, MatchPlayerStats> PlayerStats = new();
    public Dictionary<uint, SpawnPoint> SpawnPoints = new();
    public Dictionary<uint, ZonePlayerInfo> PlayerInfo = new();
    public Dictionary<TeamType, uint> Chat = new();
    public ZonePhase Phase = new()
    {
        PhaseType = ZonePhaseType.Waiting,
        StartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
    };
    public Key? MapKey;
    public required Key MatchKey;
    public required Key GameModeKey;
    public Dictionary<uint, uint?> PlayerSpawnPoints = new();
    public Dictionary<uint, ulong> RespawnInfo = new();
    public required MapData MapData;
    public required MapBinary BlocksData;
    public List<ZoneObjective> Objectives = [];
    public bool MatchEnded;
    public TeamType Winner;
    public float? ResourceCap;
    public readonly bool[] IsSurrenderRequest = new bool[Enum.GetValues<TeamType>().Length];
    public readonly Dictionary<uint, bool?> SurrenderVotes = new();
    public readonly DateTimeOffset?[] SurrenderEndTime = new DateTimeOffset?[Enum.GetValues<TeamType>().Length];
    public bool? SurrenderResult;
    public TeamType SurrenderTeam;
    public bool CanSwitchHero;
    public SupplyInfo SupplyInfo;

    public bool IsTimeTrial { get; private set; }

    public bool IsTutorial { get; private set; }

    public bool IsRankedGame { get; private set; }

    public float PlanePosition => MapData.Properties!.PlanePosition;

    public CardMatch MatchCard => Databases.Catalogue.GetCard<CardMatch>(MatchKey)!;

    public CardGameMode GameModeCard => Databases.Catalogue.GetCard<CardGameMode>(GameModeKey)!;

    public MatchTeamStats GetTeamScores(TeamType team) => 
        TeamStats.TryGetValue(team, out var matchTeamStats) ? matchTeamStats : new MatchTeamStats();

    public MatchPlayerStats GetPlayerStats(uint playerId) => 
        PlayerStats.TryGetValue(playerId, out var matchPlayerStats) ? matchPlayerStats : new MatchPlayerStats();

    public TeamType? GetPlayerTeam(uint playerId) => GetPlayerStats(playerId).Team;

    public TimeTrialCourse? GetTimeTrialCourse() => 
        MapKey.HasValue ? CatalogueHelper.TimeTrialLogic.Courses?.Find(c => c.Map == MapKey.Value) : null;

    public ZoneInitData GetZoneInitData() =>
        new()
        {
            MapKey = MapKey,
            Map = new MapData
            {
                Version = MapData.Version,
                Schema = MapData.Schema,
                Match = MapData.Match,
                ColorPalette = MapData.ColorPalette,
                SpawnPoints = MapData.SpawnPoints,
                Units = MapData.Units,
                Cameras = MapData.Cameras,
                Triggers = MapData.Triggers,
                Properties = MapData.Properties,
                Size = MapData.Size,
                BlocksData = [],
                ColorsData = []
            },
            MapData = BlocksData.ToBinary(),
            ColorData = MapData.ColorsData ?? [],
            Updates = new Dictionary<Vector3s, BlockUpdate>(),
            CanSwitchHero = CanSwitchHero,
            IsCustomGame = GameModeKey == CatalogueHelper.ModeCustom.Key
        };

    public void UpdateData(ZoneUpdate update)
    {
        if (update.Phase != null)
        {
            Phase = update.Phase;
        }
        if (update.SpawnPoints != null)
        {
            SpawnPoints.Clear();
            foreach (var spawnPoint in update.SpawnPoints)
              SpawnPoints.Add(spawnPoint.Id, spawnPoint);
        }
        if (update.PlayerSpawnPoints != null)
        {
            PlayerSpawnPoints = update.PlayerSpawnPoints;
        }
        if (update.Statistics != null)
        {
            if (update.Statistics.PlayerStats != null)
            {
                foreach (var playerStat in update.Statistics.PlayerStats)
                {
                  PlayerStats.TryGetValue(playerStat.Key, out var matchPlayerStats);
                    if (matchPlayerStats == null)
                    {
                        matchPlayerStats = new MatchPlayerStats();
                        PlayerStats.Add(playerStat.Key, matchPlayerStats);
                    }
                    if (playerStat.Value.Team.HasValue)
                        matchPlayerStats.Team = playerStat.Value.Team.Value;
                    matchPlayerStats.Assists = playerStat.Value.Assists;
                    matchPlayerStats.Deaths = playerStat.Value.Deaths;
                    matchPlayerStats.Kills = playerStat.Value.Kills;
                    matchPlayerStats.BlocksBuilt = playerStat.Value.BlocksBuilt;
                    matchPlayerStats.BlocksDestroyed = playerStat.Value.BlocksDestroyed;
                    matchPlayerStats.ResourcesEarned = playerStat.Value.ResourcesEarned;
                }
            }
            if (update.Statistics.Team1Stats != null) 
              TeamStats[TeamType.Team1] = update.Statistics.Team1Stats;
            if (update.Statistics.Team2Stats != null)
              TeamStats[TeamType.Team2] = update.Statistics.Team2Stats;
        }
        if (update.RespawnInfo != null)
          RespawnInfo = update.RespawnInfo;
        if (update.PlayerInfo != null)
          PlayerInfo = update.PlayerInfo;
        if (update.SupplyInfo != null)
          SupplyInfo = update.SupplyInfo;
        if (update.Objectives != null)
          Objectives = update.Objectives;
        if (update.ResourceCap.HasValue)
          ResourceCap = update.ResourceCap.Value;

        updater.OnZoneUpdate(update);
    }

    public void UpdateSpawn(uint spawnId, SpawnPointLockType? lockType = null, uint? ownerId = null,
        bool changeOwner = false, Vector3? position = null, TeamType? team = null)
    {
        bool hasChange;
        if (SpawnPoints.TryGetValue(spawnId, out var spawn))
        {
            hasChange = lockType is not null && spawn.Lock != lockType;
            spawn.Lock = lockType ?? spawn.Lock;
            hasChange = hasChange || (changeOwner && ownerId != spawn.Owner);
            spawn.Owner = changeOwner ? ownerId : spawn.Owner;
            hasChange = hasChange || (position is not null && position != spawn.Pos);
            spawn.Pos = position ?? spawn.Pos;
            hasChange = hasChange || (team is not null && team != spawn.Team);
            spawn.Team = team ?? spawn.Team;
        }
        else
        {
            hasChange = true;
            SpawnPoints[spawnId] = new SpawnPoint
            {
                Id = spawnId,
                Lock = lockType ?? SpawnPointLockType.Free,
                Owner = ownerId,
                Pos = position ?? Vector3.Zero,
                Team = team ?? TeamType.Neutral
            };
        }

        if (hasChange)
        {
            updater.OnZoneUpdate(new ZoneUpdate
            {
                SpawnPoints = SpawnPoints.Values.ToList()
            });
        }
    }

    public void RemoveSpawn(uint spawnId)
    {
        if (!SpawnPoints.TryGetValue(spawnId, out var spawn)) return;
        spawn.Lock = SpawnPointLockType.ServerBlocked;

        foreach (var player in PlayerSpawnPoints.Where(p => p.Value == spawnId).ToList())
        {
            PlayerSpawnPoints[player.Key] = null;
        }

        updater.OnZoneUpdate(new ZoneUpdate
        {
            SpawnPoints = SpawnPoints.Values.ToList(),
            PlayerSpawnPoints = PlayerSpawnPoints
        });
    }

    public void UpdatePlayerSelectedSpawn(uint playerId, uint? spawnId)
    {
        PlayerSpawnPoints[playerId] = spawnId;
        updater.OnZoneUpdate(new ZoneUpdate
        {
            PlayerSpawnPoints = PlayerSpawnPoints
        });
    }

    public void UpdateSpawnTime(uint playerId, ulong? spawnTime)
    {
        if (spawnTime is null)
        {
            RespawnInfo.Remove(playerId);
        }
        else
        {
            RespawnInfo[playerId] = spawnTime.Value;
        }
        updater.OnZoneUpdate(new ZoneUpdate
        {
            RespawnInfo = RespawnInfo
        });
    }

    public void UpdateSupplyTime(SupplySequenceItem supply, Vector3? position)
    {
        SupplyInfo = new SupplyInfo
        {
            NextSupplyDrop = supply.SupplyUnitKey,
            NextSupplyDropTime = (ulong)DateTimeOffset.Now.AddSeconds(supply.Seconds).ToUnixTimeMilliseconds(),
            Position = position
        };
        
        updater.OnZoneUpdate(new ZoneUpdate
        {
            SupplyInfo = SupplyInfo
        });
    }

    public void CheckIfObjective(Unit checkedUnit, TeamType captureTeam)
    {
        switch (MatchCard.Data?.Type)
        {
            case MatchType.Tutorial:
                if (checkedUnit.PickupUnitData is not null &&
                    checkedUnit.UnitCard?.Labels?.Contains(UnitLabel.TutorialCheckpoint) is true)
                {
                    ObjectiveObtained(0, TeamType.Team1);
                }
                break;
            case MatchType.TimeTrial:
                var course = GetTimeTrialCourse();
                if (course?.MatchObjectives is not { Count: > 0 })
                    return;
                
                foreach (var objective in course.MatchObjectives.Where(o => o.Team == captureTeam))
                {
                    var isObjective = objective switch
                    {
                        MatchObjectiveCollectPickups matchObjectiveCollectPickups =>
                            matchObjectiveCollectPickups.PickupKey is not null
                                ? checkedUnit.Key == matchObjectiveCollectPickups.PickupKey
                                : matchObjectiveCollectPickups.PickupLabel is not null &&
                                  checkedUnit.UnitCard?.Labels?.Contains(matchObjectiveCollectPickups.PickupLabel.Value)
                                      is true,
                        
                        MatchObjectiveKillUnits matchObjectiveKillUnits =>
                            matchObjectiveKillUnits.UnitTeam switch
                            {
                                RelativeTeamType.Both => true,
                                RelativeTeamType.Friendly => captureTeam == checkedUnit.Team,
                                RelativeTeamType.Opponent => captureTeam != checkedUnit.Team,
                                _ => false
                            } &&
                            (matchObjectiveKillUnits.UnitKey is not null
                                ? checkedUnit.Key == matchObjectiveKillUnits.UnitKey
                                : matchObjectiveKillUnits.UnitLabel is not null &&
                                  checkedUnit.UnitCard?.Labels?.Contains(matchObjectiveKillUnits.UnitLabel.Value) is
                                      true),
                        
                        _ => false
                    };

                    if (isObjective)
                    {
                        ObjectiveObtained(objective.Id, objective.Team);
                    }
                }
                break;
        }
    }

    private void ObjectiveObtained(int id, TeamType team)
    {
        Objectives.ForEach(o =>
        {
            if (o.Team == team && o.Id == id)
            {
                o.Counter += 1;
            }
        });
        
        UpdateData(new ZoneUpdate
        {
            Objectives = Objectives
        });
    }

    public void EndMatch(TeamType winner)
    {
        MatchEnded = true;
        Winner = winner;
    }

    public class Objective
    {
        public required ZoneObjective Data;
        public required MatchObjective CardData;
    }
}