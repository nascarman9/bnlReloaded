using System.Diagnostics;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;
public class ServiceLogin(ISender sender) : IServiceLogin
{
    private enum ServiceLoginId : byte
    {
        MessageCheckVersion = 0,
        MessagePing = 1,
        MessagePong = 2,
        MessageLoginDebug = 3,
        MessageLoginMaster = 4,
        MessageLoginMasterDebug = 5,
        MessageLoginMasterSteam = 6,
        MessageLoginMasterXxx = 7,
        MessageLoginMasterPpp = 8,
        MessageNeedRegion = 9,
        MessageSelectRegion = 10,
        MessageEnterRegion = 11,
        MessageLoginRegion = 12,
        MessageWait = 13,
        MessageLoggedIn = 14,
        MessageCatalogue = 15,
        MessageLoginInstance = 16
    }

    private const int ProtocolVersion = 310;
    private const int ProtocolHash = 0;
    private const string LoginToken = "wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwtest";
    
    private readonly IPlayerDatabase _playerDatabase = Databases.PlayerDatabase;
    private readonly IServerDatabase _serverDatabase = Databases.ServerDatabase;

    private uint PlayerId { get; set; }

    private static BinaryWriter CreateWriter()
    {
        var memStream =  new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceLogin);
        return writer;
    }

    public void SendCheckVersion(ushort rpcId, bool accepted, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageCheckVersion);
        writer.Write(rpcId);
        if (accepted)
        {
            writer.Write((byte) 0);
            writer.Write(accepted);
            new BitField(true, true).Write(writer);
            writer.Write(ProtocolVersion);
            writer.Write(ProtocolHash);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.SendToSession(writer);
    }
    
    private void ReceiveCheckVersion(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var bitfield = new BitField(2);
        bitfield.Read(reader);
        if (bitfield[0])
        { 
            var version = reader.ReadInt32(); 
            if (version != ProtocolVersion)
            { 
                SendCheckVersion(rpcId, false, $"client version doesn't match server version: {version}");
                return;
            }
        }
        else
        {
            SendCheckVersion(rpcId, false, "missing client version");    
            return;
        }
        
        if (bitfield[1])
        {
            var hash = reader.ReadInt32();
            if (hash != ProtocolHash)
            {
                SendCheckVersion(rpcId, false, $"client hash doesn't match server hash: {hash}");
                return;
            }    
        }
        else
        {
            SendCheckVersion(rpcId, false, "missing client hash");   
            return;
        }
        
        SendCheckVersion(rpcId, true);
    }

    private void ReceivePing()
    {
        SendPong();
    }

    public void SendPong()
    {
        using var writer = CreateWriter();    
        writer.Write((byte)ServiceLoginId.MessagePong);
        sender.SendToSession(writer);
    }

    public void SendLoginMasterSuccess(ushort rpcId, bool? serverMaintenance, bool? steamToken)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginMaster);
        writer.Write(rpcId);
        writer.Write((byte) 0);
        var bitfield = new BitField(serverMaintenance != null, steamToken != null);
        bitfield.Write(writer);
        if (serverMaintenance != null)
            writer.Write(serverMaintenance.Value);
        if (steamToken != null)
            writer.Write(steamToken.Value);
        sender.SendToSession(writer);
    }

    public void SendLoginMasterError(ushort rpcId, string error)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginMaster);
        writer.Write(rpcId);
        writer.Write(byte.MaxValue);
        writer.Write(error);
        sender.SendToSession(writer);
    }

    private void ReceiveLoginMaster(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var token = reader.ReadString();
        if (token != LoginToken)
        {  
            SendLoginMasterError(rpcId, $"invalid login token {token}");
        }
        else
        {
            SendLoginMasterSuccess(rpcId, false, true);
        }
    }
    
    public void SendLoginMasterSteam(ushort rpcId, uint? playerId, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginMasterSteam);
        writer.Write(rpcId);
        if (playerId != null)
        {
            writer.Write((byte) 0);
            writer.Write(playerId.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.SendToSessionSync(writer);
        SendRegions(_serverDatabase.GetRegionServers());
    }

    private void ReceiveLoginMasterSteam(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var loginInfo = SteamLoginInfo.ReadRecord(reader);
        PlayerId = _playerDatabase.GetPlayerId(loginInfo.SteamId);
        SendLoginMasterSteam(rpcId, PlayerId);
    }
    
    public void SendRegions(List<RegionInfo> regions, string? selected = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageNeedRegion);
        writer.WriteList(regions, RegionInfo.WriteRecord);
        writer.Write(selected ?? string.Empty);
        sender.SendToSession(writer);
    }

    private void ReceiveSelectRegion(BinaryReader reader)
    {
        var region = reader.ReadString();
        var remember = reader.ReadBoolean();
        var regionToEnter = _serverDatabase.GetRegionServer(region);
        if (regionToEnter != null)
            SendEnterRegion(regionToEnter);
    }
    
    public void SendEnterRegion(RegionInfo region)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageEnterRegion);
        writer.Write(region.Host!);
        writer.Write(region.Port);
        writer.Write(_playerDatabase.GetAuthTokenForPlayer(PlayerId));
        sender.SendToSession(writer);
    }

    public void SendLoginRegion(ushort rpcId, PlayerRole? role, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginRegion);
        writer.Write(rpcId);
        if (role != null)
        {
            writer.Write((byte) 0);
            writer.WriteByteEnum(role.Value);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error!);
        }
        sender.SendToSessionSync(writer);
    }

    private void ReceiveLoginRegion(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var auth = reader.ReadString();
        var catalogueHash = reader.ReadUInt32();
        SendLoginRegion(rpcId, PlayerRole.Admin);
        SendLoggedIn();
        SendCatalogue(null);
    }
    
    public void SendWait(float waitTime)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageWait);
        writer.Write(waitTime);
        sender.SendToSession(writer);
    }
    
    public void SendLoggedIn()
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoggedIn);
        sender.SendToSession(writer);
    }

    public void SendCatalogue(ICollection<Card>? cards)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageCatalogue);
        writer.Write(cards != null);
        if (cards != null)
            writer.WriteList(cards, Card.WriteVariant);
        sender.SendToSession(writer);
    }

    public void Receive(BinaryReader reader)
    {
        var serviceLoginId = reader.ReadByte();
        Console.WriteLine($"Service Login ID: {serviceLoginId}");
        switch (serviceLoginId)
        {
            case (byte)ServiceLoginId.MessageCheckVersion: 
                ReceiveCheckVersion(reader);
                break;
            case (byte)ServiceLoginId.MessagePing:
                ReceivePing();
                break;
            case (byte)ServiceLoginId.MessageLoginMaster:
                ReceiveLoginMaster(reader);
                break;
            case (byte)ServiceLoginId.MessageLoginMasterSteam:
                ReceiveLoginMasterSteam(reader);
                break;
            case (byte)ServiceLoginId.MessageSelectRegion:
                ReceiveSelectRegion(reader);
                break;
            case (byte)ServiceLoginId.MessageLoginRegion:
                ReceiveLoginRegion(reader);
                break;
            default:
                Console.WriteLine($"Login service received unsupported serviceId: {serviceLoginId}");
                break;
        }
    }
}