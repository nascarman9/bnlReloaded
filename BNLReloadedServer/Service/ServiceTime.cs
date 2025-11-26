using BNLReloadedServer.Database;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServiceTime(ISender sender) : IServiceTime
{
    private enum ServiceTimeId : byte
    {
        MessageSetOrigin = 0,
        MessageSync = 1
    }
    
    private static BinaryWriter CreateWriter()
    {
        var memStream =  new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceTime);
        return writer;
    }

    public void SendSetOrigin(long time)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceTimeId.MessageSetOrigin);
        writer.Write(time);
        sender.Send(writer);
    }

    public void SendSync(ushort rpcId, long? time, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceTimeId.MessageSync);
        writer.Write(rpcId);
        if (time != null)
        {
            writer.Write((byte) 0);
            writer.Write(time.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveSync(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        SendSync(rpcId, DateTimeOffset.Now.ToUnixTimeMilliseconds());
    }
    
    public void Receive(BinaryReader reader)
    {
        var serviceTimeId = reader.ReadByte();
        ServiceTimeId? timeEnum = null;
        if (Enum.IsDefined(typeof(ServiceTimeId), serviceTimeId))
        {
            timeEnum = (ServiceTimeId)serviceTimeId;
        }

        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"ServiceTimeId: {timeEnum.ToString()}");
        }

        switch (timeEnum)
        {
            case ServiceTimeId.MessageSync:
                ReceiveSync(reader);
                break;
            default:
                Console.WriteLine($"Time service received unsupported serviceId: {serviceTimeId}");
                break;
        }
    }
}