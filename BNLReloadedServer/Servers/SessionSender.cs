using System.Collections.Concurrent;
using System.Collections.Immutable;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class SessionSender : ISender
{
    private readonly AsyncTaskTcpServer _server;

    // For senders that apply to only one session, this will house the playerId
    public uint? AssociatedPlayerId { get; set; }
    
    public int SenderCount => _sessions.Count;

    private readonly IDictionary<Guid, AsyncSenderTask> _sessions;

    public SessionSender(AsyncTaskTcpServer server, Guid guid, AsyncSenderTask callingSession)
    {
        _server = server;
        _sessions = new Dictionary<Guid, AsyncSenderTask>
        {
            {guid, callingSession}
        }.ToImmutableDictionary();
    }

    public SessionSender(AsyncTaskTcpServer server)
    {
        _server = server;
        _sessions = new ConcurrentDictionary<Guid, AsyncSenderTask>();
    }

    public void Send(BinaryWriter writer)
    {
        var message = AppendMessageLength(writer);
        foreach (var session in _sessions.Values)
            session.SendPacket(message);
    }

    public void Send(byte[] buffer)
    {
        foreach (var session in _sessions.Values)
            session.SendPacket(buffer);
    }

    public void SendExcept(BinaryWriter writer, List<Guid> excluded)
    {
        var message = AppendMessageLength(writer);
        foreach (var session in _sessions.Values.Where(e => !excluded.Contains(e.Id)))
            session.SendPacket(message);
    }
    
    public void Subscribe(Guid sessionId)
    {
        var session = _server.FindAsyncSenderTask(sessionId);
        if (session != null)
            _sessions[sessionId] = session;
    }

    public void Unsubscribe(Guid sessionId) => _sessions.Remove(sessionId);

    public void UnsubscribeAll() => _sessions.Clear();

    private static byte[] AppendMessageLength(BinaryWriter writer)
    {
        var memStream = new MemoryStream();
        var baseStream = writer.BaseStream as MemoryStream;
        using var packetWriter = new BinaryWriter(memStream);
        packetWriter.Write7BitEncodedInt((int)baseStream!.Length);
        packetWriter.Write(baseStream.GetBuffer(), 0, (int)baseStream.Length);
        return (packetWriter.BaseStream as MemoryStream)!.ToArray();
    }
}