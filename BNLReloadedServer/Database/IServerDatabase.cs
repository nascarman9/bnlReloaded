using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Database;

public interface IServerDatabase
{
    public List<RegionInfo> GetRegionServers();
    public RegionInfo? GetRegionServer(string id);
}