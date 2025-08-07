namespace BNLReloadedServer.Servers;

public interface IServiceDispatcher
{  
    public void Dispatch(BinaryReader reader);
}