using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class SessionSender(TcpServer server, TcpSession callingSession) : ISender
{
    public void SendToSession(BinaryWriter writer)
    {
        callingSession.SendAsync(AppendMessageLength(writer));
    }

    public void SendToSessionSync(BinaryWriter writer)
    {
        callingSession.Send(AppendMessageLength(writer));
    }

    public void SendToSessions(BinaryWriter writer, Guid[] sessionIds)
    {
        foreach (var sessionId in sessionIds)
        {
            server.FindSession(sessionId).SendAsync(AppendMessageLength(writer));
        }
    }

    public void SendToAllSessions(BinaryWriter writer)
    {
        server.Multicast(AppendMessageLength(writer));
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