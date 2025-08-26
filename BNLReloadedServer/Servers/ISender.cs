namespace BNLReloadedServer.Servers;

public interface ISender
{
    public uint? AssociatedPlayerId { get; set; }
    public void Send(BinaryWriter writer);
    public void Send(byte[] buffer);
    public void SendSync(BinaryWriter writer);
    public void SendSync(byte[] buffer);
    public void Subscribe(Guid sessionId);
    public void Unsubscribe(Guid sessionId);
    public void UnsubscribeAll();
}