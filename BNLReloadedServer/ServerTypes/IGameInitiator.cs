using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ServerTypes;

public interface IGameInitiator
{
    public string? GameInstanceId { get; set; }
    
    public void StartIntoMatch();
    public void ClearInstance();
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
}