using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServicePlayer : IService
{
    public void SendPlayerUpdate(PlayerUpdate playerUpdate);
    public void SendServerRevision(string revision);
    public void SendRequestProfile(ushort rpcId, ProfileData? profile, string? error = null);
    public void SendSteamCurrency(ushort rpcId, string currency);
}