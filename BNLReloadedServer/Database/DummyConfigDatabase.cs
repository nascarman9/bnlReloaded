using System.Net;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Database;

public class DummyConfigDatabase : IConfigDatabase
{
    public bool IsMaster() => true;

    public bool DoToJson() => false;

    public bool DoFromJson() => true;

    public bool DoRunServer() => true;
    
    public bool UseMasterCdb() => false;
    
    public string MasterHost() => "127.0.0.1";
    public string MasterPublicHost() => "127.0.0.1";

    public IPAddress MasterIp() => IPAddress.Parse(MasterHost());

    public string RegionHost() => "127.0.0.1";
    
    public string RegionPublicHost() => "127.0.0.1";

    public IPAddress RegionIp() => IPAddress.Parse(RegionHost());
    
    public IPAddress RegionPublicIp() => IPAddress.Parse(RegionPublicHost());

    public RegionGuiInfo GetRegionInfo() => new()
    {
        Icon = "server_namericaeast",
        Name = new LocalizedString
        {
            Text = "Test",
            Data = new Dictionary<Locale, LocalizedEntry>
            {
                {
                    Locale.en, new LocalizedEntry
                    {
                        Original = "Test",
                        Translation = "Test"
                    }
                }
            }
        }
    };

    public string ToJsonCdbName() => "currCdb3.json";

    public string FromJsonCdbName() => "currCdb2.json";

    public string CdbName() => "cdb";
    
    public bool DebugMode() => true;
    public bool DoReadline() => false;

    public bool EnableMasterStatusHttp() => true;

    public int MasterStatusHttpPort() => 28104;

    public string MasterStatusHttpHost() => "localhost";

    public string MasterStatusHttpPrefix() => $"http://{MasterStatusHttpHost()}:{MasterStatusHttpPort()}/";
}
