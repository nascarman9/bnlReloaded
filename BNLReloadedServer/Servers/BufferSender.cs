namespace BNLReloadedServer.Servers;

public class BufferSender : IBufferedSender
{
    private readonly Lock _streamLock = new();
    private readonly MemoryStream _stream = new();
    private long _messageLength;
    
    public byte[] GetBuffer()
    {
        lock (_streamLock)
        {
            _stream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[_messageLength];
            _stream.ReadExactly(buffer);
            _stream.SetLength(0);
            _messageLength = 0;
            return buffer;
        }
    }

    public uint? AssociatedPlayerId { get; set; }

    public void Send(BinaryWriter writer)
    {
        var message = AppendMessageLength(writer);
        Interlocked.Add(ref _messageLength, message.Length);
        lock (_streamLock)
        {
           _stream.Write(message); 
        }
    }

    public void Send(byte[] buffer)
    {
        Interlocked.Add(ref _messageLength, buffer.Length);
        lock (_streamLock)
        {
            _stream.Write(buffer);
        }
    }

    public void SendSync(BinaryWriter writer)
    {
        var message = AppendMessageLength(writer);
        Interlocked.Add(ref _messageLength, message.Length);
        lock (_streamLock)
        {
            _stream.Write(message); 
        }
    }

    public void SendSync(byte[] buffer)
    {
        Interlocked.Add(ref _messageLength, buffer.Length);
        lock (_streamLock)
        {
            _stream.Write(buffer);
        }
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