namespace BNLReloadedServer.Service;

public interface IServicePing : IService
{
    public void SendServerPing();
    public void SendClientPong();
}