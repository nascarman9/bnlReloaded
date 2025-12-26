using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class MasterServer(IPAddress address, int port) : AsyncTaskTcpServer(address, port)
{
    protected override TcpSession CreateSession() => new MasterSession(this);

    protected override void OnStarting() => Console.WriteLine("Server starting...");

    protected override void OnStarted() => Console.WriteLine("Server started.");

    protected override void OnError(SocketError error) => 
        Console.WriteLine($"Master TCP server caught an error with code {error}");
}