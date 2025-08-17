namespace BNLReloadedServer.Servers;

public interface ISender
{
    public uint? AssociatedPlayerId { get; set; }
    public void Send(BinaryWriter writer);
    public void Send(byte[] buffer);
    public void SendSync(BinaryWriter writer);
    public void SendSync(byte[] buffer);
}