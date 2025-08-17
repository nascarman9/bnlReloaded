using BNLReloadedServer.Database;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.Servers;

public class RegionServiceDispatcher : IServiceDispatcher
{
    private readonly ServiceLogin _serviceLogin;
    private readonly ServiceScene _serviceScene;
    private readonly ServiceTime _serviceTime;
    private readonly ServiceCatalogue _serviceCatalogue;
    private readonly ServicePlayer _servicePlayer;
    private readonly ServiceChat _serviceChat;
    private readonly ServiceMatchmaker _serviceMatchmaker;

    public RegionServiceDispatcher(ISender sender, Guid sessionId)
    {
        _serviceLogin = new ServiceLogin(sender, sessionId);
        _serviceScene = new ServiceScene(sender);
        _serviceTime = new ServiceTime(sender);
        _serviceCatalogue = new ServiceCatalogue(sender);
        _servicePlayer = new ServicePlayer(sender, _serviceScene, _serviceTime);
        _serviceChat = new ServiceChat(sender);
        _serviceMatchmaker = new ServiceMatchmaker(sender);
        
        Databases.RegionServerDatabase.RegisterService(sessionId, _serviceLogin, ServiceId.ServiceLogin);
        Databases.RegionServerDatabase.RegisterService(sessionId, _serviceScene, ServiceId.ServiceScene);
        Databases.RegionServerDatabase.RegisterService(sessionId, _serviceTime, ServiceId.ServiceTime);
        Databases.RegionServerDatabase.RegisterService(sessionId, _serviceCatalogue, ServiceId.ServiceCatalogue);
        Databases.RegionServerDatabase.RegisterService(sessionId, _servicePlayer, ServiceId.ServicePlayer);
        Databases.RegionServerDatabase.RegisterService(sessionId, _serviceChat, ServiceId.ServiceChat);
        Databases.RegionServerDatabase.RegisterService(sessionId, _serviceMatchmaker, ServiceId.ServiceMatchmaker);
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
            case (byte)ServiceId.ServiceChat:
                _serviceChat.Receive(reader);
                break;
            case (byte)ServiceId.ServiceMatchmaker:
                _serviceMatchmaker.Receive(reader);
                break;
            default: 
                Console.WriteLine($"Region TCP session received unsupported serviceId: {serviceId}");
                break;
        }
    }
}