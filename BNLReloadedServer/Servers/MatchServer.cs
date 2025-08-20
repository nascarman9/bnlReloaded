using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class MatchServer(IPAddress address, int port) : TcpServer(address, port)
{
    protected override TcpSession CreateSession() { return new MatchSession(this); }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"Match TCP server caught an error with code {error}");
    }
}