namespace BNLReloadedServer.Service;

public interface IServiceTime : IService
{
    public void SendSetOrigin(long time);
    public void SendSync(ushort rpcId, long? time, string? error = null);
}