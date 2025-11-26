using System.Net;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Database;

public interface IConfigDatabase
{
    public bool IsMaster();
    public bool DoToJson();
    public bool DoFromJson();
    public bool DoRunServer();
    public bool UseMasterCdb();
    public string MasterHost();
    public IPAddress MasterIp();
    public string RegionHost();
    public IPAddress RegionIp();
    public RegionGuiInfo GetRegionInfo();
    public string ToJsonCdbName();
    public string FromJsonCdbName();
    public string CdbName();
    public bool DebugMode();
}