namespace BNLReloadedServer.Service;

public interface IServicePlayer : IService
{
    public void SendServerRevision(string revision);
    public void SendSteamCurrency(ushort rpcId, string currency);
}