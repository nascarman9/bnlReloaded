using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ServerTypes;

public interface IGameInitiator
{
    public void StartIntoMatch();
    public TeamType GetTeamForPlayer(uint playerId);
}