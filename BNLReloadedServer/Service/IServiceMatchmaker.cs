using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServiceMatchmaker : IService
{
    public void SendMatchmakerUpdate(MatchmakerUpdate matchmakerUpdate);

    public void SendQueueLeft(uint actorId);
    
    public void SendCustomGamesList(ushort rpcId, List<CustomGameInfo> customGamesList, string? error = null);
    
    public void SendJoinCustomGame(ushort rpcId, CustomGameJoinResult result, string? error = null);
    
    public void SendSpectateCustomGame(ushort rpcId, CustomGameSpectateResult result, string? error = null);
    
    public void SendUpdateCustomGame(CustomGameUpdate update);

    public void SendCustomGamePlayerKicked(uint playerId);
    
    public void SendExitCustomGame();
    
    public void SendJoinCustomGameBySteam(ushort rpcId, CustomGameJoinResult result, string? error = null);
}