using BNLReloadedServer.BaseTypes;
using Moserware.Skills;

namespace BNLReloadedServer.ServerTypes;

public interface IGameInitiator
{
    public string? GameInstanceId { get; }
    
    public void StartIntoMatch();
    public void ClearInstance(string? instanceId);
    public TeamType GetTeamForPlayer(uint playerId);
    public bool IsPlayerSpectator(uint playerId);
    public bool IsPlayerBackfill(uint playerId);
    public Key GetGameMode();
    public bool CanSwitchHero();
    public bool IsMapEditor();
    public float GetResourceCap();
    public float GetResourceAmount();
    public long? GetBuildPhaseEndTime(DateTimeOffset startTime);
    public float GetRespawnMultiplier();
    public bool IsSuperSupplies();
    public bool NeedsBackfill();
    public void SetBackfillReady(bool backfillReady);
    public (Dictionary<uint, Rating> team1, Dictionary<uint, Rating> team2) GetTeamRatings();
}