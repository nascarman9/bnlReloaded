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
    private readonly ServiceDebug _serviceDebug;
    private readonly ServiceMapEditor _serviceMapEditor;
    private readonly ServiceMatchmaker _serviceMatchmaker;
    private readonly ServiceLeaderboard _serviceLeaderboard;
    private readonly ServicePing _servicePing;

    public RegionServiceDispatcher(ISender sender, Guid sessionId)
    {
        _serviceLogin = new ServiceLogin(sender, sessionId);
        _serviceScene = new ServiceScene(sender);
        _serviceTime = new ServiceTime(sender);
        _serviceCatalogue = new ServiceCatalogue(sender);
        _servicePlayer = new ServicePlayer(sender, _serviceScene, _serviceTime);
        _serviceChat = new ServiceChat(sender);
        _serviceDebug = new ServiceDebug(sender);
        _serviceMapEditor = new ServiceMapEditor(sender);
        _serviceMatchmaker = new ServiceMatchmaker(sender);
        _serviceLeaderboard = new ServiceLeaderboard(sender);
        _servicePing = new ServicePing(sender);

        try
        {
            Databases.RegionServerDatabase.RegisterService(sessionId, _serviceLogin, ServiceId.ServiceLogin);
            Databases.RegionServerDatabase.RegisterService(sessionId, _serviceScene, ServiceId.ServiceScene);
            Databases.RegionServerDatabase.RegisterService(sessionId, _serviceTime, ServiceId.ServiceTime);
            Databases.RegionServerDatabase.RegisterService(sessionId, _serviceCatalogue, ServiceId.ServiceCatalogue);
            Databases.RegionServerDatabase.RegisterService(sessionId, _servicePlayer, ServiceId.ServicePlayer);
            Databases.RegionServerDatabase.RegisterService(sessionId, _serviceChat, ServiceId.ServiceChat);
            Databases.RegionServerDatabase.RegisterService(sessionId, _serviceDebug, ServiceId.ServiceDebug);
            Databases.RegionServerDatabase.RegisterService(sessionId, _serviceMapEditor, ServiceId.ServiceMapEditor);
            Databases.RegionServerDatabase.RegisterService(sessionId, _serviceMatchmaker, ServiceId.ServiceMatchmaker);
            Databases.RegionServerDatabase.RegisterService(sessionId, _serviceLeaderboard, ServiceId.ServiceLeaderboard);
            Databases.RegionServerDatabase.RegisterService(sessionId, _servicePing, ServiceId.ServicePing);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static bool OnUnsupported(ServiceId? serviceId)
    {
        Console.WriteLine($"Region TCP session received unsupported serviceId: {serviceId}");
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
            ServiceId.ServiceScene => _serviceScene.Receive(reader),
            ServiceId.ServiceTime => _serviceTime.Receive(reader),
            ServiceId.ServiceCatalogue => _serviceCatalogue.Receive(reader),
            ServiceId.ServicePlayer => _servicePlayer.Receive(reader),
            ServiceId.ServiceChat => _serviceChat.Receive(reader),
            ServiceId.ServiceDebug => _serviceDebug.Receive(reader),
            ServiceId.ServiceMapEditor => _serviceMapEditor.Receive(reader),
            ServiceId.ServiceMatchmaker => _serviceMatchmaker.Receive(reader),
            ServiceId.ServiceLeaderboard => _serviceLeaderboard.Receive(reader),
            ServiceId.ServicePing => _servicePing.Receive(reader),
            _ => OnUnsupported(serviceEnum)
        };
    }
}