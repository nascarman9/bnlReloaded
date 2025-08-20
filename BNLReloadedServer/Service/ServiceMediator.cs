using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServiceMediator(ISender sender) : IServiceMediator
{
    private enum ServiceMediatorId : byte
    {
        MessageEnableDisconnect = 0
    }
    
    private static BinaryWriter CreateWriter()
    {
        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceMediator);
        return writer;
    }

    public void SendEnableDisconnect()
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMediatorId.MessageEnableDisconnect);
        sender.Send(writer);
    }
    
    public void Receive(BinaryReader reader)
    {
        var serviceMediatorId = reader.ReadByte();
        Console.WriteLine($"ServiceMediatorId: {serviceMediatorId}");
        Console.WriteLine($"Mediator service received unsupported serviceId: {serviceMediatorId}");
    }

}