namespace BNLReloadedServer.Servers;

public interface ISender
{
    public uint? AssociatedPlayerId { get; set; }
    public int SenderCount { get; }
    public void Send(BinaryWriter writer);
    public void Send(byte[] buffer);
    public void SendExcept(BinaryWriter writer, List<Guid> excluded);
    public void Subscribe(Guid sessionId);
    public void Unsubscribe(Guid sessionId);
    public void UnsubscribeAll();
}