using BNLReloadedServer.Service;

namespace BNLReloadedServer.Servers;
public class MasterServiceDispatcher(ISender sender, Guid sessionId) : IServiceDispatcher
{
    private readonly ServiceLogin _serviceLogin = new(sender, sessionId);

    public void Dispatch(BinaryReader reader)
    {
        var serviceId = reader.ReadByte();
        Console.WriteLine($"Service ID: {serviceId}");
        switch (serviceId)
        {
            case (byte)ServiceId.ServiceLogin: 
                _serviceLogin.Receive(reader);
                break;
            
            default: 
                Console.WriteLine($"Master TCP session received unsupported serviceId: {serviceId}");
                break;
        }
    }
}