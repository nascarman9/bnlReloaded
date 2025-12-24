using System.Collections.Concurrent;
using System.Collections.Immutable;
using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class SessionSender : ISender
{
    private readonly TcpServer _server;

    // For senders that apply to only one session, this will house the playerId
    public uint? AssociatedPlayerId { get; set; }
    
    public int SenderCount => _sessions.Count;

    private readonly IDictionary<Guid, TcpSession> _sessions;

    public SessionSender(TcpServer server, Guid guid, TcpSession callingSession)
    {
        _server = server;
        _sessions = new Dictionary<Guid, TcpSession>
        {
            {guid, callingSession}
        }.ToImmutableDictionary();
    }

    public SessionSender(TcpServer server)
    {
        _server = server;
        _sessions = new ConcurrentDictionary<Guid, TcpSession>();
    }

    public void Send(BinaryWriter writer)
    {
        var message = AppendMessageLength(writer);
        foreach (var session in _sessions.Values)
            session.SendAsync(message);
    }

    public void Send(byte[] buffer)
    {
        foreach (var session in _sessions.Values)
            session.SendAsync(buffer);
    }

    public void Send(ReadOnlySpan<byte> buffer)
    {
        foreach (var session in _sessions.Values)
            session.SendAsync(buffer);
    }

    public void SendExcept(BinaryWriter writer, List<Guid> excluded)
    {
        var message = AppendMessageLength(writer);
        foreach (var session in _sessions.Values.Where(e => !excluded.Contains(e.Id)))
            session.SendAsync(message);
    }

    public void SendSync(BinaryWriter writer)
    {
        var message = AppendMessageLength(writer);
        foreach (var session in _sessions.Values)
            session.Send(message);
    }

    public void SendSync(byte[] buffer)
    {
        foreach (var session in _sessions.Values)
            session.Send(buffer);
    }

    public void SendSync(ReadOnlySpan<byte> buffer)
    {
        foreach (var session in _sessions.Values)
            session.Send(buffer);
    }

    public void Subscribe(Guid sessionId)
    {
        var session = _server.FindSession(sessionId);
        if (session != null)
            _sessions[sessionId] = session;
    }

    public void Unsubscribe(Guid sessionId)
    {
        _sessions.Remove(sessionId);
    }

    public void UnsubscribeAll()
    {
        _sessions.Clear();
    }

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