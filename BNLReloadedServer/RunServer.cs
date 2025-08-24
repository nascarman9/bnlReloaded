using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;

const bool masterMode = true;
const bool toJson = false;
const bool fromJson = false;
const bool runServer = true;

if (toJson || fromJson)
{
    var serializedPath = Path.Combine(Directory.GetParent(CatalogueCache.CachePath).FullName, "currCdb.json");
    var deserializedPath = Path.Combine(Directory.GetParent(CatalogueCache.CachePath).FullName, "currCdbCompressed");
    if (toJson)
    {
        var cards = Databases.Catalogue.All;
        using (var fs = new StreamWriter(File.Create(serializedPath)))
        {
            fs.Write(JsonSerializer.Serialize(cards, JsonHelper.DefaultSerializerSettings).Replace("\\u00A0", "\u00A0"));
        }
    }
    if (fromJson)
    {
        using var fs = new StreamReader(File.OpenRead(serializedPath));
        var deserializedCards = JsonSerializer.Deserialize<List<Card>>(fs.ReadToEnd(), JsonHelper.DefaultSerializerSettings);
        for (var i = 0; i < deserializedCards.Count; i++)
        {
            deserializedCards[i].Key = Catalogue.Key(deserializedCards[i].Id);
        }
        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        using var fs2 = File.Create(deserializedPath);
        writer.Write((byte)0);
        writer.WriteList(deserializedCards, Card.WriteVariant);
        var zipped = (writer.BaseStream as MemoryStream).GetBuffer().Zip(0);
        zipped.CopyTo(fs2);
        zipped.Close();
    }
}

if (runServer)
{
    MasterServer? server;
    if (masterMode)
    {
        // Create a new TCP server
        server = new MasterServer(IPAddress.Parse("127.0.0.1"), 28100);
        
        // Start the server
        Console.Write("Server starting...");
        server.Start();
        Console.WriteLine("Done!");
        Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");
    }

    var regionServer = new RegionServer(IPAddress.Parse("127.0.0.1"), 28101);
    regionServer.OptionNoDelay = true;
    regionServer.OptionReceiveBufferSize = 10000000;
    regionServer.OptionSendBufferSize = 10000000;
    var matchServer = new MatchServer(IPAddress.Parse("127.0.0.1"), 28102);
    matchServer.OptionNoDelay = true;
    matchServer.OptionReceiveBufferSize = 10000000;
    matchServer.OptionSendBufferSize = 10000000;
    Databases.RegionServerDatabase = new RegionServerDatabase(regionServer, matchServer);

    Console.Write("Region server starting...");
    regionServer.Start();
    Console.WriteLine("Done!");
    
    Console.Write("Match server starting...");
    matchServer.Start();
    Console.WriteLine("Done!");

    try 
    {
        // Perform text input
        for (;;)
        {
            var line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
                break;

            // Restart the server
            if (line == "!")
            {
                Console.Write("Server restarting...");
                if (masterMode)
                    server.Restart();
                regionServer.Restart();
                matchServer.Restart();
                Console.WriteLine("Done!");
            }
        }
    }
    finally
    {
        // Stop the server
        Console.Write("Server stopping...");
        if (masterMode)
            server.Stop();
        regionServer.Stop();
        matchServer.Stop();
        Console.WriteLine("Done!");
    }
}
