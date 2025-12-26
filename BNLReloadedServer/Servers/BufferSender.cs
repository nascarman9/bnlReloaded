using System.Buffers;

namespace BNLReloadedServer.Servers;

public class BufferSender : IBufferedSender
{
    private readonly MemoryStream _stream = new();
    private long _messageLength;
    
    public void UseBuffer(Action<byte[]> callback)
    {
        _stream.Seek(0, SeekOrigin.Begin);
        var buffer = new byte[_messageLength];
        _stream.ReadAtLeast(buffer, (int)_messageLength, false);
        _stream.SetLength(0);
        _messageLength = 0;
        
        if (buffer.Length > 0)
            callback.Invoke(buffer);
    }

    public uint? AssociatedPlayerId { get; set; }

    public int SenderCount => 0;

    public void Send(BinaryWriter writer)
    {
        var message = AppendMessageLength(writer);
        _messageLength += message.Length;
        _stream.Write(message); 
    }

    public void Send(byte[] buffer)
    {
        _messageLength += buffer.Length;
        _stream.Write(buffer);
    }

    public void SendExcept(BinaryWriter writer, List<Guid> excluded)
    {
        var message = AppendMessageLength(writer);
        _messageLength += message.Length;
        _stream.Write(message);
    }

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
        var baseStream = writer.BaseStream as MemoryStream;
        using var packetWriter = new BinaryWriter(memStream);
        packetWriter.Write7BitEncodedInt((int)baseStream!.Length);
        packetWriter.Write(baseStream.GetBuffer(), 0, (int)baseStream.Length);
        return (packetWriter.BaseStream as MemoryStream)!.ToArray();
    }
}