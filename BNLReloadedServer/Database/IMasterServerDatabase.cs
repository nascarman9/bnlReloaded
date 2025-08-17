using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Database;

public interface IMasterServerDatabase
{
    public List<RegionInfo> GetRegionServers();
    public RegionInfo? GetRegionServer(string id);
}