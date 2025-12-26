using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class RegionServer(IPAddress address, int port) : AsyncTaskTcpServer(address, port)
{
    protected override TcpSession CreateSession() => new RegionSession(this);

    protected override void OnStarting() => Console.WriteLine("Region server starting...");

    protected override void OnStarted() => Console.WriteLine("Region server started.");

    protected override void OnError(SocketError error) => 
        Console.WriteLine($"Region TCP server caught an error with code {error}");
}