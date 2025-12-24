namespace BNLReloadedServer.Servers;

public interface ISender
{
    public uint? AssociatedPlayerId { get; set; }
    public int SenderCount { get; }
    public void Send(BinaryWriter writer);
    public void Send(byte[] buffer);
    public void Send(ReadOnlySpan<byte> buffer);
    public void SendExcept(BinaryWriter writer, List<Guid> excluded);
    public void SendSync(BinaryWriter writer);
    public void SendSync(byte[] buffer);
    public void SendSync(ReadOnlySpan<byte> buffer);
    public void Subscribe(Guid sessionId);
    public void Unsubscribe(Guid sessionId);
    public void UnsubscribeAll();
}