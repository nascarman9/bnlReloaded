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
    public string MasterPublicHost();
    public IPAddress MasterIp();
    public string RegionHost();
    public string RegionPublicHost();
    public IPAddress RegionIp();
    public IPAddress RegionPublicIp();
    public RegionGuiInfo GetRegionInfo();
    public string ToJsonCdbName();
    public string FromJsonCdbName();
    public string CdbName();
    public bool DebugMode();
    public bool DoReadline();
    public bool EnableMasterStatusHttp();
    public int MasterStatusHttpPort();
    public string MasterStatusHttpHost();
    public string MasterStatusHttpPrefix();
}
