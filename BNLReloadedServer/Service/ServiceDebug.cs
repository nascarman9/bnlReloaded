using System.Numerics;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;

public class ServiceDebug(ISender sender) : IServiceDebug
{
    private enum ServiceDebugId : byte
    {
        MessageExecute = 0,
        MessageExecuteArgs = 1,
        MessageGetScreenshot = 2,
        MessageLoginCore = 3,
        MessageGetNodeTree = 4, 
        MessageCoreCommand = 5,
        MessageFileListing = 6,
        MessageLoadFile = 7,
        MessageSaveFile = 8, 
        MessageGetTriggers = 9,
        MessageSubscribeZone = 10,
        MessageZoneAddSplash = 11,
        MessageZoneUnitMoved = 12,
        MessageZoneUnitRemoved = 13,
        MessageZoneTriggerMoved = 14,
        MessageZoneTriggerRemoved = 15
    }
    
    private static BinaryWriter CreateWriter()
    {
        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceDebug);
        return writer;
    }

    public void SendExecute(ushort rpcId, string? result, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageExecute);
        writer.Write(rpcId);
        if (result != null)
        {
            writer.Write((byte) 0);
            writer.Write(result);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveExecute(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var cmd = reader.ReadString();
    }

    public void SendExecuteArgs(ushort rpcId, string? result, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageExecuteArgs);
        writer.Write(rpcId);
        if (result != null)
        {
            writer.Write((byte) 0);
            writer.Write(result);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveExecuteArgs(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var cmd = reader.ReadString();
        var args = reader.ReadList<string, List<string>>(reader.ReadString);
    }

    public void SendGetScreenshot(ushort rpcId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageGetScreenshot);
        writer.Write(rpcId);
        sender.Send(writer);
    }

    private void ReceiveGetScreenshot(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        switch (reader.ReadByte())
        {
            case 0:
                var data = reader.ReadBinary();
                break;
            case byte.MaxValue:
                var error = reader.ReadString();
                break;
        }
    }

    private void ReceiveLoginCore(BinaryReader reader)
    {
        var pwd = reader.ReadString();
    }

    public void SendGetNodeTree(ushort rpcId, DebugServerNode? node, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageGetNodeTree);
        writer.Write(rpcId);
        if (node != null)
        {
            writer.Write((byte) 0);
            DebugServerNode.WriteRecord(writer, node);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveGetNodeTree(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
    }

    public void SendCoreCommand(ushort rpcId, string? result, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageCoreCommand);
        writer.Write(rpcId);
        if (result != null)
        {
            writer.Write((byte) 0);
            writer.Write(result);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveCoreCommand(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var nodes = reader.ReadList<string, List<string>>(reader.ReadString);
        var cmd = reader.ReadString();
    }

    public void SendFileListing(ushort rpcId, List<string>? data, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageFileListing);
        writer.Write(rpcId);
        if (data != null)
        {
            writer.Write((byte) 0);
            writer.WriteList(data, writer.Write);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveFileListing(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var nodes = reader.ReadList<string, List<string>>(reader.ReadString);
    }

    public void SendLoadFile(ushort rpcId, byte[]? data, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageLoadFile);
        writer.Write(rpcId);
        if (data != null)
        {
            writer.Write((byte) 0);
            writer.WriteBinary(data);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveLoadFile(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var nodes = reader.ReadList<string, List<string>>(reader.ReadString);
        var path = reader.ReadString();
    }

    public void SendSaveFile(ushort rpcId, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageSaveFile);
        writer.Write(rpcId);
        if (error == null)
        {
            writer.Write((byte) 0);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error);
        }
        sender.Send(writer);
    }

    private void ReceiveSaveFile(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var nodes = reader.ReadList<string, List<string>>(reader.ReadString);
        var path = reader.ReadString();
        var data = reader.ReadBinary();
    }

    public void SendGetTriggers(ushort rpcId, Dictionary<int, List<Vector3s>>? triggers, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageGetTriggers);
        writer.Write(rpcId);
        if (triggers != null)
        {
            writer.Write((byte) 0);
            writer.WriteMap(triggers, writer.Write, item => writer.WriteList(item, writer.Write));
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveGetTriggers(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
    }

    private void ReceiveSubscribeZone(BinaryReader reader)
    {
        
    }

    public void SendZoneAddSplash(Vector3s hitPos, float radius, float damage, Dictionary<Vector3s, float> blocks)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageZoneAddSplash);
        writer.Write(hitPos);
        writer.Write(radius);
        writer.Write(damage);
        writer.WriteMap(blocks, writer.Write, writer.Write);
        sender.Send(writer);
    }

    public void SendZoneUnitMoved(int unitId, Vector3 pos, List<Vector3s> blocks)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageZoneUnitMoved);
        writer.Write(unitId);
        writer.Write(pos);
        writer.WriteList(blocks, writer.Write);
        sender.Send(writer);
    }

    public void SendZoneUnitRemoved(int unitId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageZoneUnitRemoved);
        writer.Write(unitId);
        sender.Send(writer);
    }

    public void SendZoneTriggerMoved(int triggerId, List<Vector3s> blocks)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageZoneTriggerMoved);
        writer.Write(triggerId);
        writer.WriteList(blocks, writer.Write);
        sender.Send(writer);
    }

    public void SendZoneTriggerRemoved(int triggerId)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceDebugId.MessageZoneTriggerRemoved);
        writer.Write(triggerId);
        sender.Send(writer);
    }
    
    public void Receive(BinaryReader reader)
    {
        var serviceDebugId = reader.ReadByte();
        ServiceDebugId? debugEnum = null;
        if (Enum.IsDefined(typeof(ServiceDebugId), serviceDebugId))
        {
            debugEnum = (ServiceDebugId)serviceDebugId;
        }

        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"ServiceDebugId: {serviceDebugId}");
        }

        switch (debugEnum)
        {
            case ServiceDebugId.MessageExecute:
                ReceiveExecute(reader);
                break;
            case ServiceDebugId.MessageExecuteArgs:
                ReceiveExecuteArgs(reader);
                break;
            case ServiceDebugId.MessageGetScreenshot:
                ReceiveGetScreenshot(reader);
                break;
            case ServiceDebugId.MessageLoginCore:
                ReceiveLoginCore(reader);
                break;
            case ServiceDebugId.MessageGetNodeTree:
                ReceiveGetNodeTree(reader);
                break;
            case ServiceDebugId.MessageCoreCommand:
                ReceiveCoreCommand(reader);
                break;
            case ServiceDebugId.MessageFileListing:
                ReceiveFileListing(reader);
                break;
            case ServiceDebugId.MessageLoadFile:
                ReceiveLoadFile(reader);
                break;
            case ServiceDebugId.MessageSaveFile:
                ReceiveSaveFile(reader);
                break;
            case ServiceDebugId.MessageGetTriggers:
                ReceiveGetTriggers(reader);
                break;
            case ServiceDebugId.MessageSubscribeZone:
                ReceiveSubscribeZone(reader);
                break;
            default:
                Console.WriteLine($"Unknown service debug id {serviceDebugId}");
                break;
        }
    }
}