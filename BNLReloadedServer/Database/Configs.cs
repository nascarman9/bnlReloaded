using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Database;

public class Configs
{
    public bool IsMaster { get; init; }
    public bool RunServer { get; init; }
    public bool UseMasterCdb { get; init; }
    public string? CdbName { get; init; }
    public required string MasterHost { get; init; }
    public required string MasterPublicHost { get; init; }
    public required string RegionHost { get; init; }
    public required string RegionPublicHost { get; init; }
    public required string RegionName { get; init; }
    public required string RegionIcon { get; init; }
    public bool ToJson { get; init; }
    public string? ToJsonName { get; init; }
    public bool FromJson { get; init; }
    public string? FromJsonName { get; init; }
    public bool DebugMode { get; init; }
    public bool DoReadline { get; init; }
    public bool EnableMasterStatusHttp { get; init; } = true;
    public int MasterStatusHttpPort { get; init; } = 28104;
    public string MasterStatusHttpHost { get; init; } = "localhost";
}
