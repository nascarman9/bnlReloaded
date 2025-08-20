using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServiceLobby : IService
{
    public void SendLobbyUpdate(LobbyUpdate update);
    public void SendClearLobby();
    public void SendMatchLoadingProgress(Dictionary<uint, float> playersProgress);
}