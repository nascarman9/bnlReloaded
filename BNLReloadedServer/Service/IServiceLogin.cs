using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServiceLogin : IService
{
    public void SendCheckVersion(ushort rpcId, bool accepted, string? error = null);
    public void SendPong();
    public void SendLoginDebug(ushort rpcId, string? node, EAuthFailed? authFailed = null, string? error = null);
    public void SendLoginMasterSuccess(ushort rpcId, bool? serverMaintenance, bool? steamToken);
    public void SendLoginMasterError(ushort rpcId, string error);
    public void SendLoginMasterDebug(ushort rpcId, uint? id2, EAuthFailed? authFailed = null, string? error = null);
    public void SendLoginMasterSteam(ushort rpcId, uint? playerId, EAuthFailed? authFailed = null, EContentAuthFailed? contentAuthFailed = null, string? error = null);
    public void SendLoginMasterXxx(ushort rpcId, uint? id2, EAuthFailed? authFailed = null, string? error = null);
    public void SendLoginMasterPpp(ushort rpcId, uint? id2, EAuthFailed? authFailed = null, string? error = null);
    public void SendRegions(List<RegionInfo> regions, string? selected = null);
    public void SendEnterRegion(RegionInfo region);
    public void SendLoginRegion(ushort rpcId, PlayerRole? role, EAuthFailed? authFailed = null, string? error = null);
    public void SendWait(float waitTime);
    public void SendLoggedIn();
    public void SendCatalogue(ICollection<Card>? cards);
    public void SendLoginInstance(ushort rpcId, EAuthFailed? authFailed = null, string? error = null);
}