using BNLReloadedServer.Service;

namespace BNLReloadedServer.Servers;
public class MasterServiceDispatcher(ISender sender, Guid sessionId) : IServiceDispatcher
{
    private readonly ServiceLogin _serviceLogin = new(sender, sessionId);

    public void Dispatch(BinaryReader reader)
    {
        var serviceId = reader.ReadByte();
        ServiceId? serviceEnum = null;
        if (Enum.IsDefined(typeof(ServiceId), serviceId))
        {
            serviceEnum = (ServiceId)serviceId;
        }
        Console.WriteLine($"Service ID: {serviceEnum.ToString()}");
        switch (serviceEnum)
        {
            case ServiceId.ServiceLogin: 
                _serviceLogin.Receive(reader);
                break;
            default: 
                Console.WriteLine($"Master TCP session received unsupported serviceId: {serviceId}");
                break;
        }
    }
}