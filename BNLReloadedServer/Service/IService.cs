namespace BNLReloadedServer.Service;

public interface IService
{
    public bool Receive(BinaryReader reader);
}