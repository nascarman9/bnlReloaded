using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServiceScene(ISender sender) : IServiceScene
{
    private enum ServiceSceneId : byte
    {
        MessageChangeScene = 0,
        MessageEnterScene = 1,
        MessageEnterInstance = 2,
        MessageServerUpdate = 3
    }
    
    private static BinaryWriter CreateWriter()
    {
        var memStream =  new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceScene);
        return writer;
    }

    public void SendChangeScene(Scene scene)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceSceneId.MessageChangeScene);
        Scene.WriteVariant(writer, scene);
        sender.Send(writer);
    }

    private void ReceiveEnterScene(BinaryReader reader)
    {
        if (sender.AssociatedPlayerId.HasValue)
        {
            Databases.RegionServerDatabase.UserEnterScene(sender.AssociatedPlayerId.Value);
        }
    }

    public void SendEnterInstance(string host, int port, string auth)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceSceneId.MessageEnterInstance);
        writer.Write(host);
        writer.Write(port);
        writer.Write(auth);
        sender.Send(writer);
    }

    public void SendServerUpdate(ServerUpdate update)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceSceneId.MessageServerUpdate);
        ServerUpdate.WriteRecord(writer, update);
        sender.Send(writer);
    }
    
    public bool Receive(BinaryReader reader)
    {
        var serviceSceneId = reader.ReadByte();
        ServiceSceneId? sceneEnum = null;
        if (Enum.IsDefined(typeof(ServiceSceneId), serviceSceneId))
        {
            sceneEnum = (ServiceSceneId)serviceSceneId;
        }

        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"ServiceSceneId: {sceneEnum.ToString()}");
        }

        switch (sceneEnum)
        {
            case ServiceSceneId.MessageEnterScene:
                ReceiveEnterScene(reader);
                break;
            default:
                Console.WriteLine($"Scene service received unsupported serviceId: {serviceSceneId}");
                return false;
        }
        
        return true;
    }
}