using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServicePing(ISender sender) : IServicePing
{
    private enum ServicePingId : byte
    {
        MessageServerPing = 0,
        MessageServerPong = 1,
        MessageClientPing = 2,
        MessageClientPong = 3
    }
    
    private static BinaryWriter CreateWriter()
    {
        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServicePing);
        return writer;
    }
    
    public void SendServerPing()
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePingId.MessageServerPing);
        sender.Send(writer);
    }

    private void ReceiveServerPong(BinaryReader reader)
    {  
        
    }

    private void ReceiveClientPing(BinaryReader reader)
    {
        SendClientPong();
    }

    public void SendClientPong()
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServicePingId.MessageClientPong);
        sender.Send(writer);
    }
    
    public void Receive(BinaryReader reader)
    {
        var servicePingId = reader.ReadByte();
        Console.WriteLine($"ServicePingId: {servicePingId}");
        switch (servicePingId)
        {
            case (byte)ServicePingId.MessageServerPong:
                ReceiveServerPong(reader);
                break;
            case (byte)ServicePingId.MessageClientPing:
                ReceiveClientPing(reader);
                break;
            default:
                Console.WriteLine($"Unknown service ping id {servicePingId}");
                break;
        }
    }
}