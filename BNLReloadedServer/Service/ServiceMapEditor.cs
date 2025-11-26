using System.Text.Json;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BNLReloadedServer.Service;

public class ServiceMapEditor(ISender sender) : IServiceMapEditor
{
    private enum ServiceMapEditorId : byte
    {
        MessageLoadMap = 0,
        MessageSaveMap = 1,
        MessageLoadMetadata = 2,
        MessageEncodeMap = 3,
        MessageDecodeMap = 4, 
        MessageCheckMap = 5,
        MessagePlayMapData = 6,
        MessagePlayMapKey = 7
    }
    
    private readonly IRegionServerDatabase _serverDatabase = Databases.RegionServerDatabase;
    
    private static BinaryWriter CreateWriter()
    {
        var memStream =  new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceMapEditor);
        return writer;
    }

    public void SendLoadMap(ushort rpcId, MapData? map, byte[]? blocks = null, byte[]? colors = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMapEditorId.MessageLoadMap);
        writer.Write(rpcId);
        if (map != null)
        {
            writer.Write((byte) 0);
            MapData.WriteRecord(writer, map);
            writer.WriteOption(blocks, writer.WriteBinary);
            writer.WriteOption(colors, writer.WriteBinary);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveLoadMap(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var key = reader.ReadString();

        var mapData = Databases.MapDatabase.LoadMapData(new Key(key));
        SendLoadMap(rpcId, mapData, mapData?.BlocksData, mapData?.ColorsData);
    }

    public void SendSaveMap(ushort rpcId, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMapEditorId.MessageSaveMap);
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

    private void ReceiveSaveMap(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var key = reader.ReadString();
        var map = MapData.ReadRecord(reader);
    }

    public void SendLoadMetadata(ushort rpcId, HerculesMetadata? metadata, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMapEditorId.MessageLoadMetadata);
        writer.Write(rpcId);
        if (metadata != null)
        {
            writer.Write((byte) 0);
            HerculesMetadata.WriteRecord(writer, metadata);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveLoadMetadata(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var key = reader.ReadString();
    }

    public void SendEncodeMap(ushort rpcId, string? signedMap, EMapValidation? mapValidation = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMapEditorId.MessageEncodeMap);
        writer.Write(rpcId);
        if (signedMap != null)
        {
            writer.Write((byte) 0);
            writer.Write(signedMap);
        }
        else if (mapValidation != null)
        {
            writer.Write((byte) 1);
            EMapValidation.WriteRecord(writer, mapValidation);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveEncodeMap(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var map = MapData.ReadRecord(reader);
    }

    public void SendDecodeMap(ushort rpcId, MapData? map, byte[]? blocks = null, byte[]? colors = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMapEditorId.MessageDecodeMap);
        writer.Write(rpcId);
        if (map != null)
        {
            writer.Write((byte) 0);
            MapData.WriteRecord(writer, map);
            writer.WriteOption(blocks, writer.WriteBinary);
            writer.WriteOption(colors, writer.WriteBinary);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveDecodeMap(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var signedMap = reader.ReadString();
        
        var handler = new JsonWebTokenHandler();
        var jsonWebToken = handler.ReadJsonWebToken(signedMap);
            
        var rawPayload = jsonWebToken.EncodedPayload;
        var mapJson = Base64UrlEncoder.Decode(rawPayload);
            
        var mapData = JsonSerializer.Deserialize<MapData>(mapJson, JsonHelper.DefaultSerializerSettings);
        
        SendDecodeMap(rpcId, mapData, mapData?.BlocksData, mapData?.ColorsData);
    }

    public void SendCheckMap(ushort rpcId, EMapValidation? mapValidation = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMapEditorId.MessageCheckMap);
        writer.Write(rpcId);
        if (mapValidation == null && error == null)
        {
            writer.Write((byte) 0);
        }
        else if (mapValidation != null)
        {
            writer.Write((byte) 1);
            EMapValidation.WriteRecord(writer, mapValidation);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceiveCheckMap(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var map = MapData.ReadRecord(reader);
        
        SendCheckMap(rpcId);
    }

    public void SendPlayMapData(ushort rpcId, EMapValidation? mapValidation = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMapEditorId.MessagePlayMapData);
        writer.Write(rpcId);
        if (mapValidation == null && error == null)
        {
            writer.Write((byte) 0);
        }
        else if (mapValidation != null)
        {
            writer.Write((byte) 1);
            EMapValidation.WriteRecord(writer, mapValidation);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.Send(writer);
    }

    private void ReceivePlayMapData(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var map = MapData.ReadRecord(reader);
        var hero = Key.ReadRecord(reader);
        var team = reader.ReadByteEnum<TeamType>();
        
        SendPlayMapData(rpcId);
        if (sender.AssociatedPlayerId.HasValue)
        {
            _serverDatabase.StartMapEditorGame(sender.AssociatedPlayerId.Value, map, hero, team);
        }
    }

    private void ReceivePlayMapKey(BinaryReader reader)
    {
        var mapKey = Key.ReadRecord(reader);
        var hero = Key.ReadRecord(reader);
        var team = reader.ReadByteEnum<TeamType>();

        var mapData = Databases.MapDatabase.LoadMapData(mapKey);
        if (sender.AssociatedPlayerId.HasValue && mapData != null)
        {
            _serverDatabase.StartMapEditorGame(sender.AssociatedPlayerId.Value, mapData, hero, team);
        }
    }
    
    public void Receive(BinaryReader reader)
    {
        var serviceMapEditorId = reader.ReadByte();
        ServiceMapEditorId? mapEditorEnum = null;
        if (Enum.IsDefined(typeof(ServiceMapEditorId), serviceMapEditorId))
        {
            mapEditorEnum = (ServiceMapEditorId)serviceMapEditorId;
        }

        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"ServiceMapEditorId: {mapEditorEnum.ToString()}");
        }

        switch (mapEditorEnum)
        {
            case ServiceMapEditorId.MessageLoadMap:
                ReceiveLoadMap(reader);
                break;
            case ServiceMapEditorId.MessageSaveMap:
                ReceiveSaveMap(reader);
                break;
            case ServiceMapEditorId.MessageLoadMetadata:
                ReceiveLoadMetadata(reader);
                break;
            case ServiceMapEditorId.MessageEncodeMap:
                ReceiveEncodeMap(reader);
                break;
            case ServiceMapEditorId.MessageDecodeMap:
                ReceiveDecodeMap(reader);
                break;
            case ServiceMapEditorId.MessageCheckMap:
                ReceiveCheckMap(reader);
                break;
            case ServiceMapEditorId.MessagePlayMapData:
                ReceivePlayMapData(reader);
                break;
            case ServiceMapEditorId.MessagePlayMapKey:
                ReceivePlayMapKey(reader);
                break;
            default:
                Console.WriteLine($"Unknown service MapEditor id {serviceMapEditorId}");
                break;
        }
    }
}