using System.Net.Sockets;
using BNLReloadedServer.Database;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

internal class MasterSession : TcpSession
{
    private readonly SessionReader _reader;
    private bool _connected;

    public MasterSession(AsyncTaskTcpServer server) : base(server)
    {
        var senderTask = new AsyncSenderTask(this);
        server.AddSenderTask(Id,  senderTask);
        var sender = new SessionSender(server, Id, senderTask);
        var serviceDispatcher = new MasterServiceDispatcher(sender, Id);
        _reader = new SessionReader(serviceDispatcher, Databases.ConfigDatabase.DebugMode(),
            "Master server received packet with incorrect length");
    }

    protected override void OnConnected()
    {
        _connected = true;
        Console.WriteLine($"Master TCP session with Id {Id} connected!");
    }

    protected override void OnDisconnected()
    {
        if (_connected)
            Console.WriteLine($"Master TCP session with Id {Id} disconnected!");

        _connected = false;

        Databases.MasterServerDatabase.RemoveRegionServer(Id.ToString());
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        if (size <= 0) return;
        
        _reader.ProcessPacket(buffer, offset, size);
    }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"Master TCP session caught an error with code {error}");
    }
}