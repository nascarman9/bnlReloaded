using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class MasterServer(IPAddress address, int port) : TcpServer(address, port)
{
    protected override TcpSession CreateSession() { return new MasterSession(this); }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"Master TCP server caught an error with code {error}");
    }
}