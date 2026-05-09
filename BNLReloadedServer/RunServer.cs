using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;
using BNLReloadedServer.Service;
using CouchDB.Driver;
using CouchDB.Driver.Options;

var configs = Databases.ConfigDatabase;
var masterMode = configs.IsMaster();
var toJson = configs.DoToJson();
var fromJson = configs.DoFromJson();
var runServer = configs.DoRunServer();
var useCouch = configs.UseCouchDb();

const int bufferSize = 2000000;  // 2MB

var toPath = Path.Combine(Databases.CacheFolderPath, configs.ToJsonCdbName());
var deserializedPath = Path.Combine(Databases.CacheFolderPath, configs.CdbName());
CatalogueStore catalogueStore = useCouch
    ? new CouchCatalogueStore(
        new CouchClient(configs.CouchDbEndpoint(), configs.CouchDbCredentials(),
            new CouchClientOptions
            {
                JsonSerializerOptions = JsonHelper.DefaultSerializerSettings
            }),
        configs.CouchDbDatabaseName(),
        toPath,
        deserializedPath,
        JsonHelper.DefaultSerializerSettings)
    : new JsonCatalogueStore(
        Path.Combine(Databases.CacheFolderPath, configs.FromJsonCdbName()),
        toPath,
        deserializedPath,
        JsonHelper.DefaultSerializerSettings);

if (toJson)
    catalogueStore.Store(Databases.Catalogue.All);
if (fromJson)
    catalogueStore.Load(Databases.MapDatabase.GetMapCards(), Databases.MapDatabase.GrabExtraMaps());

if (runServer)
{
    MasterServer? server = null;
    if (masterMode)
    {
        // Create a new TCP server
        server = new MasterServer(configs.MasterIp(), 28100);
        server.OptionSendBufferSize = bufferSize;
        server.OptionReceiveBufferSize = bufferSize;
        
        // Start the server
        server.Start();
    }

    var regionServer = new RegionServer(configs.RegionIp(), 28101);
    regionServer.OptionNoDelay = true;
    regionServer.OptionSendBufferSize = bufferSize;
    regionServer.OptionReceiveBufferSize = bufferSize;
    var regionClient = new RegionClient(configs.MasterHost(), 28100);
    regionClient.OptionNoDelay = true;
    regionClient.OptionSendBufferSize = bufferSize;
    regionClient.OptionReceiveBufferSize = bufferSize;
    var matchServer = new MatchServer(configs.RegionIp(), 28102);
    matchServer.OptionNoDelay = true;
    matchServer.OptionSendBufferSize = bufferSize;
    matchServer.OptionReceiveBufferSize = bufferSize;
    Databases.SetRegionDatabase(new RegionServerDatabase(regionServer, matchServer));
   
    regionServer.Start();
    regionClient.ConnectAsync();
    matchServer.Start();
    
    Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");
    try
    {
        // Perform text input
        while (true)
        {
            if (Databases.ConfigDatabase.DoReadline())
            {
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;

                switch (line)
                {
                    // Restart the server
                    case "!":
                    {
                        Console.Write("Server restarting...");
                        server?.Restart();
                        regionServer.Restart();
                        regionClient.Disconnect();
                        regionClient.Reconnect();
                        matchServer.Restart();
                        Console.WriteLine("Done!");
                        break;
                    }
                    case "refreshCdb" or "refreshCdbLoad" when Databases.Catalogue is ServerCatalogue serverCatalogue:
                    {
                        if (line == "refreshCdbLoad")
                        {
                            catalogueStore.Load(Databases.MapDatabase.GetMapCards(), Databases.MapDatabase.GrabExtraMaps());
                        }
                        Console.Write("Refreshing cdb...");
                        var newCardList = CatalogueCache.UpdateCatalogue(CatalogueCache.Load());
                        serverCatalogue.Replicate(newCardList);
                        var catalogueReplicator = new ServiceCatalogue(new ServerSender(regionServer));
                        catalogueReplicator.SendReplicate(newCardList);
                        Console.WriteLine("Done!");
                        break;
                    }
                }
            }
            else
            {
                Task.Delay(Timeout.InfiniteTimeSpan).Wait();
            }
        }
    }
    finally
    {
        // Stop the server
        Console.Write("Server stopping...");
        server?.Stop();
        regionServer.Stop();
        regionClient.DisconnectAndStop();
        if (configs.IsMaster())
        {
            Databases.MasterServerDatabase.RemoveRegionServer("master");
        }
        matchServer.Stop();
        Console.WriteLine("Done!");
    }
}
