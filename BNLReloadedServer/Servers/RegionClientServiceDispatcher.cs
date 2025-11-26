using BNLReloadedServer.Database;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.Servers;

public class RegionClientServiceDispatcher(ISender sender)
{
    public ServiceRegionServer ServiceRegionServer { get; } = new(sender);

    public void Dispatch(BinaryReader reader)
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

        switch (serviceEnum)
        {
            case ServiceId.ServiceServer:
                ServiceRegionServer.Receive(reader);
                break;
            default:
                Console.WriteLine($"Region client TCP session received unsupported serviceId: {serviceId}");
                break;
        }
    }
}