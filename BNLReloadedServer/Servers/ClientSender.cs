using NetCoreServer;

namespace BNLReloadedServer.Servers;

public class ClientSender(TcpClient client) : ISender
{
    public uint? AssociatedPlayerId { get; set; }

    public int SenderCount => 1;

    public void Send(BinaryWriter writer) => client.SendAsync(AppendMessageLength(writer));

    public void Send(byte[] buffer) => client.SendAsync(buffer);
    
    public void Send(ReadOnlySpan<byte> buffer) => client.SendAsync(buffer);

    public void SendExcept(BinaryWriter writer, List<Guid> excluded) => client.SendAsync(AppendMessageLength(writer));

    public void SendSync(BinaryWriter writer) => client.Send(AppendMessageLength(writer));

    public void SendSync(byte[] buffer) => client.Send(buffer);
    
    public void SendSync(ReadOnlySpan<byte> buffer) => client.Send(buffer);

    public void Subscribe(Guid sessionId)
    {
    }

    public void Unsubscribe(Guid sessionId)
    {
    }

    public void UnsubscribeAll()
    {
    }
    
    private static byte[] AppendMessageLength(BinaryWriter writer)
    {
        var memStream = new MemoryStream();
        var baseStream = (MemoryStream)writer.BaseStream;
        using var packetWriter = new BinaryWriter(memStream);
        packetWriter.Write7BitEncodedInt((int)baseStream.Length);
        packetWriter.Write(baseStream.GetBuffer(), 0, (int)baseStream.Length);
        return ((MemoryStream)packetWriter.BaseStream).ToArray();
    }
}