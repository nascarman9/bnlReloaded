using BNLReloadedServer.Service;

namespace BNLReloadedServer.Servers;

public class RegionServiceDispatcher : IServiceDispatcher
{
    private readonly ServiceLogin _serviceLogin;
    private readonly ServiceScene _serviceScene;
    private readonly ServiceTime _serviceTime;
    private readonly ServiceCatalogue _serviceCatalogue;
    private readonly ServicePlayer _servicePlayer;

    public RegionServiceDispatcher(ISender sender)
    {
        _serviceLogin = new ServiceLogin(sender);
        _serviceScene = new ServiceScene(sender);
        _serviceTime = new ServiceTime(sender);
        _serviceCatalogue = new ServiceCatalogue(sender);
        _servicePlayer = new ServicePlayer(sender, _serviceScene, _serviceTime);
    }

    public void Dispatch(BinaryReader reader)
    {
        var serviceId = reader.ReadByte();
        Console.WriteLine($"Service ID: {serviceId}");
        switch (serviceId)
        {
            case (byte)ServiceId.ServiceLogin: 
                _serviceLogin.Receive(reader);
                break;
            case (byte)ServiceId.ServiceScene:
                _serviceScene.Receive(reader);
                break;
            case (byte)ServiceId.ServiceTime:
                _serviceTime.Receive(reader);
                break;
            case (byte)ServiceId.ServiceCatalogue:
                _serviceCatalogue.Receive(reader);
                break;
            case (byte)ServiceId.ServicePlayer:
                _servicePlayer.Receive(reader);
                break;
            default: 
                Console.WriteLine($"Region TCP session received unsupported serviceId: {serviceId}");
                break;
        }
    }
}