using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Database;

public class DummyServerDatabase :  IServerDatabase
{
    private List<RegionInfo>? _regionServers;
    
    public List<RegionInfo> GetRegionServers()
    {
        if (_regionServers != null) return _regionServers;
        _regionServers = [];
        var region1 = new RegionInfo
        {
            Host = "127.0.0.1",
            Port = 28101,
            Id = "naeast",
            Info = new RegionGuiInfo
            {
                Icon = "server_namericaeast",
                Name = new LocalizedString
                {
                    Text = "Test",
                    Data = new Dictionary<Locale, LocalizedEntry>
                    {
                        {Locale.en, new LocalizedEntry
                        {
                            Original = "Test",
                            Translation = "Test"
                        }}
                    }
                }
            }
        };
        
        var region2 = new RegionInfo
        {
            Host = "127.0.0.1",
            Port = 28101,
            Id = "nawest",
            Info = new RegionGuiInfo
            {
                Icon = "server_namericawest",
                Name = new LocalizedString
                {
                    Text = "Test2",
                    Data = new Dictionary<Locale, LocalizedEntry>
                    {
                        {Locale.en, new LocalizedEntry
                        {
                            Original = "Test2",
                            Translation = "Test2"
                        }}
                    }
                }
            }
        };
        _regionServers.Add(region1);
        _regionServers.Add(region2);
        return _regionServers;
    }

    public RegionInfo? GetRegionServer(string id)
    {
        _regionServers ??= GetRegionServers();
        return _regionServers.FirstOrDefault(x => x.Id == id);
    }
}