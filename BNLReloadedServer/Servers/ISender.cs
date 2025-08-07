namespace BNLReloadedServer.Servers;

public interface ISender
{
    public void SendToSession(BinaryWriter writer);
    public void SendToSessionSync(BinaryWriter writer);
    public void SendToSessions(BinaryWriter writer, Guid[] sessionIds);
    public void SendToAllSessions(BinaryWriter writer);
}