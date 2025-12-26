using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class MatchServer(IPAddress address, int port) : AsyncTaskTcpServer(address, port)
{
    protected override TcpSession CreateSession() => new MatchSession(this);

    protected override void OnStarting() => Console.WriteLine("Match server starting...");

    protected override void OnStarted() => Console.WriteLine("Match server started.");

    protected override void OnError(SocketError error) => 
        Console.WriteLine($"Match TCP server caught an error with code {error}");
}