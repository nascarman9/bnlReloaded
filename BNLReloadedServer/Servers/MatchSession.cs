using System.Net.Sockets;
using BNLReloadedServer.Database;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class MatchSession : TcpSession
{
    private readonly SessionReader _reader;
    private bool _connected;

    public MatchSession(AsyncTaskTcpServer server) : base(server)
    {
        var senderTask = new AsyncSenderTask(this);
        server.AddSenderTask(Id, senderTask);
        var sender = new SessionSender(server, Id, senderTask);
        var serviceDispatcher = new MatchServiceDispatcher(sender, Id);
        _reader = new SessionReader(serviceDispatcher, Databases.ConfigDatabase.DebugMode(),
            "Match server received packet with incorrect length");
    }

    protected override void OnConnected()
    {
        _connected = true;
        Console.WriteLine($"Match TCP session with Id {Id} connected!");
    }

    protected override void OnDisconnected()
    {
        Databases.RegionServerDatabase.RemoveMatchServices(Id);
        if (_connected)
            Console.WriteLine($"Match TCP session with Id {Id} disconnected!");

        _connected = false;
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        if (size <= 0) return;
        
        _reader.ProcessPacket(buffer, offset, size);
    }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"Match TCP session caught an error with code {error}");
    }
}