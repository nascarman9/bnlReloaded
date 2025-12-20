using BNLReloadedServer.Database;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.Servers;

public class RegionClientServiceDispatcher(ISender sender) : IServiceDispatcher
{
    public ServiceRegionServer ServiceRegionServer { get; } = new(sender);

    private static bool OnUnsupported(ServiceId? serviceId)
    {
        Console.WriteLine($"Region client TCP session received unsupported serviceId: {serviceId}");
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
            ServiceId.ServiceServer => ServiceRegionServer.Receive(reader),
            _ => OnUnsupported(serviceEnum)
        };
    }
}