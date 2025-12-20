namespace BNLReloadedServer.Servers;

public interface IServiceDispatcher
{  
    public bool Dispatch(BinaryReader reader);
}