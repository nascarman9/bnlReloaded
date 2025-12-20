using BNLReloadedServer.Database;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.Servers;
public class MasterServiceDispatcher(ISender sender, Guid sessionId) : IServiceDispatcher
{
    private readonly ServiceLogin _serviceLogin = new(sender, sessionId);
    private readonly ServiceMasterServer _serviceMasterServer = new(sender, sessionId);

    private static bool OnUnsupported(ServiceId? serviceId)
    {
        Console.WriteLine($"Master TCP session received unsupported serviceId: {serviceId}");
        return false;
    }
    
    public bool Dispatch(BinaryReader reader)
    {
        var serviceId = reader.ReadByte();
        ServiceId? serviceEnum = null;
        if (Enum.IsDefined(typeof(ServiceId), serviceId))
        {
            serviceEnum = (ServiceId)serviceId;
        }

        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"Service ID: {serviceEnum.ToString()}");
        }

        return serviceEnum switch
        {
            ServiceId.ServiceLogin => _serviceLogin.Receive(reader),
            ServiceId.ServiceServer => _serviceMasterServer.Receive(reader),
            _ => OnUnsupported(serviceEnum)
        };
    }
}