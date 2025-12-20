using Moserware.Skills;

namespace BNLReloadedServer.Database;

public static class Databases
{
    public const double DefaultMean = 25.0;
    public const double DefaultSd = 25.0 / 3.0;
    public const double DefaultBeta = 25.0 / 6.0;
    public const double DefaultDynamicFactor = 1.0 / 12.0;
    
    public static string BaseFolderPath { get; } = Directory.GetCurrentDirectory();
    public static string ConfigsFolderPath { get; } = Path.Combine(BaseFolderPath, "Configs");
    public static string ConfigsFilePath { get; } = Path.Combine(ConfigsFolderPath, "configs.json");
    public static string CacheFolderPath { get; } = Path.Combine(BaseFolderPath, "Cache");
    public static string PlayerDatabaseFile { get; } = Path.Combine(BaseFolderPath, "PlayerData", "playerData.db");
    
    private static readonly Lazy<IPlayerDatabase> LazyPlayer = new(() => new PlayerDatabase());
    private static readonly Lazy<IMasterServerDatabase> LazyServer = new(() => new MasterServerDatabase());
    private static readonly Lazy<Catalogue> LazyCatalogue = new(() => new ServerCatalogue());
    private static readonly Lazy<IMapDatabase> LazyMapDatabase = new(() => new MapDatabase());
    private static readonly Lazy<IConfigDatabase> LazyConfigDatabase = new(() => new ConfigDatabase());
    
    public static IPlayerDatabase PlayerDatabase => LazyPlayer.Value;
    public static IMapDatabase MapDatabase => LazyMapDatabase.Value;
    public static IConfigDatabase ConfigDatabase => LazyConfigDatabase.Value;
    public static IMasterServerDatabase MasterServerDatabase => LazyServer.Value;
    public static IRegionServerDatabase RegionServerDatabase { get; private set; }
    public static Catalogue Catalogue => LazyCatalogue.Value;
    public static GameInfo DefaultGameInfo { get; } = new(DefaultMean, DefaultSd, DefaultBeta, DefaultDynamicFactor, 0);
    
    public static void SetRegionDatabase(IRegionServerDatabase regionServerDatabase) => 
        RegionServerDatabase = regionServerDatabase;
}