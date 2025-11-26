using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;

namespace BNLReloadedServer.ServerTypes;

public class DummyGameInitiator(CardGameMode gameMode, MapData map, TeamType team, bool mapEditor) : Updater, IGameInitiator
{
    public CardMatch MatchCard { get; } = CatalogueHelper.GetMatch(map.Match, gameMode.Key);

    public string? GameInstanceId { get; set; }

    public void StartIntoMatch()
    {
    }

    public void ClearInstance()
    {
    }

    public TeamType GetTeamForPlayer(uint playerId) => team;

    public bool IsPlayerSpectator(uint playerId) => false;

    public bool IsPlayerBackfill(uint playerId) => false;

    public Key GetGameMode() => gameMode.Key;

    public bool CanSwitchHero() => false;

    public bool IsMapEditor() => mapEditor;

    public float GetResourceCap() => MatchCard.ResourceCap ?? 7500f;

    public float GetResourceAmount() => map.Properties?.StartingResources ?? MatchCard.InitResource;

    public long? GetBuildPhaseEndTime(DateTimeOffset startTime) => 
        startTime.AddSeconds((long)(map.Properties?.BuildTime ?? MatchCard.Data switch
        {
            MatchDataShieldCapture matchDataShieldCapture => matchDataShieldCapture.Build1Time,
            MatchDataShieldRush2 matchDataShieldRush2 => matchDataShieldRush2.Build1Time,
            MatchDataTimeTrial matchDataTimeTrial => matchDataTimeTrial.PrestartTime,
            MatchDataTutorial matchDataTutorial => matchDataTutorial.BuildTime,
            _ => 0
        })).ToUnixTimeMilliseconds();

    public float GetRespawnMultiplier() => 0;
    
    public bool IsSuperSupplies() => false;
}