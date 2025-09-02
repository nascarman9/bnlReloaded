using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ServerTypes;

public interface IGameInitiator
{
    public void StartIntoMatch();
    public void ClearInstance();
    public TeamType GetTeamForPlayer(uint playerId);
    public bool IsPlayerSpectator(uint playerId);
    public Key GetGameMode();
    public bool CanSwitchHero();
    public bool IsMapEditor();
    public float GetResourceCap();
    public float GetResourceAmount();
    public long? GetBuildPhaseEndTime(DateTimeOffset startTime);
}