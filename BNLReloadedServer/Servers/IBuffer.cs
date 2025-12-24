namespace BNLReloadedServer.Servers;

public interface IBuffer
{
    public void UseBuffer(Action<ReadOnlySpan<byte>> callback);
}