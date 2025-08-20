using BNLReloadedServer.Service;

namespace BNLReloadedServer.Servers;

public class MatchServiceDispatcher : IServiceDispatcher
{
    private readonly ServiceLogin _serviceLogin;
    private readonly ServiceZone _serviceZone;
    private readonly ServiceLobby _serviceLobby;
    private readonly ServicePing _servicePing;
    private readonly ServiceMediator _serviceMediator;
    public MatchServiceDispatcher(ISender sender, Guid sessionId)
    {
        _serviceLogin = new ServiceLogin(sender, sessionId);
        _serviceZone = new ServiceZone(sender);
        _serviceLobby = new ServiceLobby(sender);
        _servicePing = new ServicePing(sender);
        _serviceMediator = new ServiceMediator(sender);
    }

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
            case ServiceId.ServiceZone:
                _serviceZone.Receive(reader);
                break;
            case ServiceId.ServiceLobby:
                _serviceLobby.Receive(reader);
                break;
            case ServiceId.ServicePing:
                _servicePing.Receive(reader);
                break;
            case ServiceId.ServiceMediator:
                _serviceMediator.Receive(reader);
                break;
            default:
                Console.WriteLine($"Match TCP session received unsupported serviceId: {serviceId}");
                break;
        }
    }
}