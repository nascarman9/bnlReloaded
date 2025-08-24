using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class SessionSender(IDictionary<Guid, TcpSession> callingSession) : ISender
{
    // For senders that apply to only one session, this will house the playerId
    public uint? AssociatedPlayerId { get; set; }

    public SessionSender(Guid guid, TcpSession callingSession) : this( new Dictionary<Guid, TcpSession> {{guid, callingSession}} )
    {
    }

    public void Send(BinaryWriter writer)
    {
        var message = AppendMessageLength(writer);
        foreach (var session in callingSession.Values)
            session.SendAsync(message);
    }

    public void Send(byte[] buffer)
    {
        foreach (var session in callingSession.Values)
            session.SendAsync(buffer);
    }

    public void SendSync(BinaryWriter writer)
    {
        var message = AppendMessageLength(writer);
        foreach (var session in callingSession.Values)
            session.Send(message);
    }

    public void SendSync(byte[] buffer)
    {
        foreach (var session in callingSession.Values)
            session.Send(buffer);
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