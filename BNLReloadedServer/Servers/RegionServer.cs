using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class RegionServer(IPAddress address, int port) : TcpServer(address, port)
{
    protected override TcpSession CreateSession() { return new RegionSession(this); }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"Region TCP server caught an error with code {error}");
    }
}