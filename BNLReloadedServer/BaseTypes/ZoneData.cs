using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneData
{
    public Dictionary<TeamType, MatchTeamStats> TeamStats = new();
    public Dictionary<uint, MatchPlayerStats> PlayerStats = new();
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
    public List<ZoneObjective> Objectives = [];
    public bool MatchEnded;
    public TeamType Winner;
    public EndMatchData EndMatchData;
    public float? ResourceCap;
    public bool IsSurrenderRequest;
    public Dictionary<uint, bool?> SurrenderVotes = new();
    public long? SurrenderEndTime;
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

    public MatchTeamStats GetTeamScores(TeamType team)
    {
      return TeamStats.TryGetValue(team, out var matchTeamStats) ? matchTeamStats : new MatchTeamStats();
    }

    public MatchPlayerStats GetPlayerStats(uint playerId)
    {
      return PlayerStats.TryGetValue(playerId, out var matchPlayerStats) ? matchPlayerStats : new MatchPlayerStats();
    }

    public TeamType? GetPlayerTeam(uint playerId)
    {
      return GetPlayerStats(playerId).Team;
    }

    public TimeTrialCourse GetTimeTrialCourse()
    {
      return MapKey.HasValue ? CatalogueHelper.TimeTrialLogic.Courses.Find((Predicate<TimeTrialCourse>) (c => c.Map == MapKey.Value)) : (TimeTrialCourse) null;
    }

    public ZoneInitData GetZoneInitData()
    {
        var mapBinary = new MapBinary(MapData.Schema, MapData.BlocksData ?? MapLoader.GetBlockData(MapKey) ?? [],
            MapData.Size);
        
        return new ZoneInitData
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
            MapData = mapBinary.ToBinary(),
            ColorData = MapData.ColorsData ?? MapLoader.GetColorData(MapKey) ?? [],
            Updates = new Dictionary<Vector3s, BlockUpdate>(),
            CanSwitchHero = CanSwitchHero,
            IsCustomGame = GameModeKey == CatalogueHelper.ModeCustom.Key
        };
    }

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