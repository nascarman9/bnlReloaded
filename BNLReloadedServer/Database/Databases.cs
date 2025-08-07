namespace BNLReloadedServer.Database;

public static class Databases
{
    private static readonly Lazy<IPlayerDatabase> LazyPlayer = new(() => new DummyPlayerDatabase());
    private static readonly Lazy<IServerDatabase> LazyServer = new(() => new DummyServerDatabase());
    private static readonly Lazy<Catalogue> LazyCatalogue = new(() => new ServerCatalogue());
    public static IPlayerDatabase PlayerDatabase => LazyPlayer.Value;
    public static IServerDatabase ServerDatabase => LazyServer.Value;
    public static Catalogue Catalogue => LazyCatalogue.Value;
    
}