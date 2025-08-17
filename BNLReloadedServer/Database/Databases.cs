using NetCoreServer;

namespace BNLReloadedServer.Database;

public static class Databases
{
    private static readonly Lazy<IPlayerDatabase> LazyPlayer = new(() => new DummyPlayerDatabase());
    private static readonly Lazy<IMasterServerDatabase> LazyServer = new(() => new MasterServerDatabase());
    private static readonly Lazy<Catalogue> LazyCatalogue = new(() => new ServerCatalogue());
    public static IPlayerDatabase PlayerDatabase => LazyPlayer.Value;
    public static IMasterServerDatabase MasterServerDatabase => LazyServer.Value;

    public static IRegionServerDatabase RegionServerDatabase { get; set; }

    public static Catalogue Catalogue => LazyCatalogue.Value;
}