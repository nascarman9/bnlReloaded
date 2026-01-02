using System.Net;
using System.Text.Json;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.Database;

public class ConfigDatabase : IConfigDatabase
{
    private readonly Configs _configs;
    private readonly IPAddress _masterIp;
    private readonly IPAddress _regionIp;
    private readonly IPAddress _regionPublicIp;
    private readonly string _masterStatusHttpHost;
    private readonly int _masterStatusHttpPort;
    private readonly bool _enableMasterStatusHttp;
    
    public ConfigDatabase()
    {
        var configs = JsonSerializer.Deserialize<Configs>(File.ReadAllText(Databases.ConfigsFilePath), JsonHelper.DefaultSerializerSettings);
        _configs = configs ?? throw new FileNotFoundException("Configs file not found");
        _masterIp = IPAddress.Parse(_configs.MasterHost);
        _regionIp = IPAddress.Parse(_configs.RegionHost);
        _regionPublicIp = IPAddress.Parse(_configs.RegionPublicHost);
        _masterStatusHttpHost = string.IsNullOrWhiteSpace(_configs.MasterStatusHttpHost) ? "*" : _configs.MasterStatusHttpHost;
        _masterStatusHttpPort = _configs.MasterStatusHttpPort is > 0 and < 65536 ? _configs.MasterStatusHttpPort : 28104;
        _enableMasterStatusHttp = _configs.EnableMasterStatusHttp;
    }

    public bool IsMaster() => _configs.IsMaster;
    
    public bool DoToJson() => _configs is { ToJson: true, ToJsonName: not null };

    public bool DoFromJson() => _configs is { FromJson: true, FromJsonName: not null };

    public bool DoRunServer() => _configs.RunServer;

    public bool UseMasterCdb() => _configs.UseMasterCdb || _configs.CdbName is null;

    public string MasterHost() => _configs.MasterHost;
    public string MasterPublicHost() => _configs.MasterPublicHost;

    public IPAddress MasterIp() => _masterIp;

    public string RegionHost() => _configs.RegionHost;
    
    public string RegionPublicHost() => _configs.RegionPublicHost;

    public IPAddress RegionIp() => _regionIp;

    public IPAddress RegionPublicIp() => _regionPublicIp;

    public RegionGuiInfo GetRegionInfo() => new()
    {
        Icon = _configs.RegionIcon,
        Name = new LocalizedString
        {
            Text = _configs.RegionName,
            Data = new Dictionary<Locale, LocalizedEntry>
            {
                {
                    Locale.en, new LocalizedEntry
                    {
                        Original = _configs.RegionName,
                        Translation = _configs.RegionName
                    }
                }
            }
        }
    };

    public string ToJsonCdbName() => _configs.ToJsonName ?? string.Empty;

    public string FromJsonCdbName() => _configs.FromJsonName ?? string.Empty;

    public string CdbName() => UseMasterCdb() && !IsMaster() ? "cdb" : _configs.CdbName ?? string.Empty;

    public bool DebugMode() => _configs.DebugMode;
    
    public bool DoReadline() => _configs.DoReadline;

    public bool EnableMasterStatusHttp() => _enableMasterStatusHttp;

    public int MasterStatusHttpPort() => _masterStatusHttpPort;

    public string MasterStatusHttpHost() => _masterStatusHttpHost;

    public string MasterStatusHttpPrefix() => $"http://{_masterStatusHttpHost}:{_masterStatusHttpPort}/";
}
