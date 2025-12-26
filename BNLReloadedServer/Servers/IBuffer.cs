namespace BNLReloadedServer.Servers;

public interface IBuffer
{
    public void UseBuffer(Action<byte[]> callback);
}