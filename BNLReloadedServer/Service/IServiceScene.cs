using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServiceScene : IService
{
    public void SendChangeScene(Scene scene);
    public void SendEnterInstance(string host, int port, string auth);
    public void SendServerUpdate(ServerUpdate update);
}