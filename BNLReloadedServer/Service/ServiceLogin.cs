using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;

namespace BNLReloadedServer.Service;
public class ServiceLogin(ISender sender, Guid sessionId) : IServiceLogin
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
    private readonly IMasterServerDatabase _masterServerDatabase = Databases.MasterServerDatabase;
    private readonly IRegionServerDatabase _regionServerDatabase = Databases.RegionServerDatabase;

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
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
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
        sender.Send(writer);
    }

    public void SendLoginDebug(ushort rpcId, string? node, EAuthFailed? authFailed = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginDebug);
        writer.Write(rpcId);
        if (node != null)
        {
            writer.Write((byte) 0);
            writer.Write(node);
        }
        else if (authFailed != null)
        {
            writer.Write((byte) 1);
            EAuthFailed.WriteRecord(writer, authFailed);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveLoginDebug(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var token = reader.ReadString();
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
        sender.Send(writer);
    }

    public void SendLoginMasterError(ushort rpcId, string error)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginMaster);
        writer.Write(rpcId);
        writer.Write(byte.MaxValue);
        writer.Write(error);
        sender.Send(writer);
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
    
    public void SendLoginMasterDebug(ushort rpcId, uint? id2, EAuthFailed? authFailed = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginMasterDebug);
        writer.Write(rpcId);
        if (id2.HasValue)
        {
            writer.Write((byte) 0);
            writer.Write(id2.Value);
        }
        else if (authFailed != null)
        {
            writer.Write((byte) 1);
            EAuthFailed.WriteRecord(writer, authFailed);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveLoginMasterDebug(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var name = reader.ReadString();
        var token = reader.ReadString();
    }
    
    public void SendLoginMasterSteam(ushort rpcId, uint? playerId, EAuthFailed? authFailed = null, EContentAuthFailed? contentAuthFailed = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginMasterSteam);
        writer.Write(rpcId);
        if (playerId != null)
        {
            writer.Write((byte) 0);
            writer.Write(playerId.Value);
        }
        else if (authFailed != null)
        {
            writer.Write((byte) 1);
            EAuthFailed.WriteRecord(writer, authFailed);
        }
        else if (contentAuthFailed != null)
        {
            writer.Write((byte) 2);
            EContentAuthFailed.WriteRecord(writer, contentAuthFailed);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
        
        var regionServers = _masterServerDatabase.GetRegionServers();
        if (regionServers.Count == 1 && sender.AssociatedPlayerId.HasValue)
        {
            EnterRegion(sender.AssociatedPlayerId.Value, regionServers.Single());
        }
        else
        {
            SendRegions(regionServers);
        }
    }
    
    private void ReceiveLoginMasterSteam(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var loginInfo = SteamLoginInfo.ReadRecord(reader);
        var player = _masterServerDatabase.GetPlayer(loginInfo.SteamId).Result;
        sender.AssociatedPlayerId = player?.PlayerId ??
                                    _masterServerDatabase.AddPlayer(loginInfo.SteamId, string.Empty, "master").Result
                                        .PlayerId;
        SendLoginMasterSteam(rpcId, sender.AssociatedPlayerId);
    }
    
    public void SendLoginMasterXxx(ushort rpcId, uint? id2, EAuthFailed? authFailed = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginMasterXxx);
        writer.Write(rpcId);
        if (id2.HasValue)
        {
            writer.Write((byte) 0);
            writer.Write(id2.Value);
        }
        else if (authFailed != null)
        {
            writer.Write((byte) 1);
            EAuthFailed.WriteRecord(writer, authFailed);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveLoginMasterXxx(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var id = reader.ReadString();
        var token = reader.ReadString();
    }
    
    public void SendLoginMasterPpp(ushort rpcId, uint? id2, EAuthFailed? authFailed = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginMasterPpp);
        writer.Write(rpcId);
        if (id2.HasValue)
        {
            writer.Write((byte) 0);
            writer.Write(id2.Value);
        }
        else if (authFailed != null)
        {
            writer.Write((byte) 1);
            EAuthFailed.WriteRecord(writer, authFailed);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }
    
    private void ReceiveLoginMasterPpp(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var id = reader.ReadBinary();
        var token = reader.ReadString();
    }
    
    public void SendRegions(List<RegionInfo> regions, string? selected = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageNeedRegion);
        writer.WriteList(regions, RegionInfo.WriteRecord);
        writer.Write(selected ?? string.Empty);
        sender.Send(writer);
    }

    private void EnterRegion(uint playerId, RegionInfo region)
    {
        if (region.Id == null || _masterServerDatabase.GetPlayer(playerId).Result is not { } player) return;
        
        if (!_masterServerDatabase.SetRegionForPlayer(player.PlayerId, region.Info?.Name?.Text ?? region.Id).Result) return;
        
        _masterServerDatabase.HaveRegionLoadPlayer(region.Id, player);
        SendEnterRegion(region, playerId);
    }

    private void ReceiveSelectRegion(BinaryReader reader)
    {
        var region = reader.ReadString();
        var remember = reader.ReadBoolean();
        var regionToEnter = _masterServerDatabase.GetRegionServer(region);
        if (sender.AssociatedPlayerId.HasValue && regionToEnter is not null)
        {
            EnterRegion(sender.AssociatedPlayerId.Value, regionToEnter);
        }
    }
    
    public void SendEnterRegion(RegionInfo region, uint playerId)
    {
        if (region.Host == null)
        {
            return;
        }
        
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageEnterRegion);
        writer.Write(region.Host);
        writer.Write(region.Port);
        writer.Write(_playerDatabase.GetAuthTokenForPlayer(playerId));
        sender.Send(writer);
    }

    public void SendLoginRegion(ushort rpcId, PlayerRole? role, EAuthFailed? authFailed = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginRegion);
        writer.Write(rpcId);
        if (role != null)
        {
            writer.Write((byte) 0);
            writer.WriteByteEnum(role.Value);
        }
        else if (authFailed != null)
        {
            writer.Write((byte)1);
            EAuthFailed.WriteRecord(writer, authFailed);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveLoginRegion(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        sender.AssociatedPlayerId = _playerDatabase.GetPlayerIdFromAuthTokenMaster(reader.ReadString());
        var catalogueHash = reader.ReadUInt32();
        if (sender.AssociatedPlayerId == null)
        {
            SendLoginRegion(rpcId, null, new EAuthFailed());
            return;
        }
        _regionServerDatabase.AddUser(sender.AssociatedPlayerId.Value, sessionId);
        var player = _playerDatabase.GetPlayerData(sender.AssociatedPlayerId.Value).Result;
        SendLoginRegion(rpcId, player.Role);
        SendLoggedIn();
        SendCatalogue(catalogueHash != CatalogueCache.Hash() ? CatalogueCache.Load() : null);
    }
    
    public void SendWait(float waitTime)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageWait);
        writer.Write(waitTime);
        sender.Send(writer);
    }
    
    public void SendLoggedIn()
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoggedIn);
        sender.Send(writer);
    }

    public void SendCatalogue(byte[]? cards)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageCatalogue);
        writer.WriteOption(cards, item => writer.WriteBinary(item));
        sender.Send(writer);
    }

    public void SendLoginInstance(ushort rpcId, EAuthFailed? authFailed = null, string? error = null)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceLoginId.MessageLoginInstance);
        writer.Write(rpcId);
        if (authFailed == null && error == null)
        {
            writer.Write((byte)0);
        }
        else if (authFailed != null)
        {
            writer.Write((byte)1);
            EAuthFailed.WriteRecord(writer, authFailed);
        }
        else
        {
            writer.Write(byte.MaxValue);
            writer.Write(error ?? string.Empty);
        }
        sender.Send(writer);
    }

    private void ReceiveLoginInstance(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var auth = reader.ReadString();
        sender.AssociatedPlayerId = _playerDatabase.GetPlayerIdFromAuthTokenRegion(auth);
        if (sender.AssociatedPlayerId == null)
        {
            SendLoginInstance(rpcId, new EAuthFailed());
        }
        else
        {
            SendLoginInstance(rpcId);
            _regionServerDatabase.LinkMatchSessionGuidToUser(sender.AssociatedPlayerId.Value, sessionId);
        }
    }

    public bool Receive(BinaryReader reader)
    {
        var serviceLoginId = reader.ReadByte();
        ServiceLoginId? loginEnum = null;
        if (Enum.IsDefined(typeof(ServiceLoginId), serviceLoginId))
        {
            loginEnum = (ServiceLoginId)serviceLoginId;
        }

        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"Service Login ID: {loginEnum.ToString()}");
        }

        switch (loginEnum)
        {
            case ServiceLoginId.MessageCheckVersion: 
                ReceiveCheckVersion(reader);
                break;
            case ServiceLoginId.MessagePing:
                ReceivePing();
                break;
            case ServiceLoginId.MessageLoginDebug:
                ReceiveLoginDebug(reader);
                break;
            case ServiceLoginId.MessageLoginMaster:
                ReceiveLoginMaster(reader);
                break;
            case ServiceLoginId.MessageLoginMasterDebug:
                ReceiveLoginMasterDebug(reader);
                break;
            case ServiceLoginId.MessageLoginMasterSteam:
                ReceiveLoginMasterSteam(reader);
                break;            
            case ServiceLoginId.MessageLoginMasterXxx:
                ReceiveLoginMasterXxx(reader);
                break;
            case ServiceLoginId.MessageLoginMasterPpp:
                ReceiveLoginMasterPpp(reader);
                break;
            case ServiceLoginId.MessageSelectRegion:
                ReceiveSelectRegion(reader);
                break;
            case ServiceLoginId.MessageLoginRegion:
                ReceiveLoginRegion(reader);
                break;
            case ServiceLoginId.MessageLoginInstance:
                ReceiveLoginInstance(reader);
                break;
            default:
                Console.WriteLine($"Login service received unsupported serviceId: {serviceLoginId}");
                return false;
        }
        
        return true;
    }
}