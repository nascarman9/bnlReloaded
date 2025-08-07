using System.Net;
using BNLReloadedServer;
using BNLReloadedServer.Servers;

const bool masterMode = true;

MasterServer? server;
if (masterMode) {
    // Create a new TCP server
    server = new MasterServer(IPAddress.Parse("127.0.0.1"), 28100); 
    
    // Start the server
    Console.Write("Server starting...");
    server.Start();
    Console.WriteLine("Done!");
    Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");
}

var regionServer = new RegionServer(IPAddress.Parse("127.0.0.1"), 28101);

Console.Write("Region server starting...");
regionServer.Start();
Console.WriteLine("Done!");

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
        Console.WriteLine("Done!");
    }
}

// Stop the server
Console.Write("Server stopping...");
if (masterMode)
    server.Stop();
regionServer.Stop();
Console.WriteLine("Done!");
