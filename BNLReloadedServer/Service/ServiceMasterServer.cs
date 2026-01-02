using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;
using Moserware.Skills;

namespace BNLReloadedServer.Service;

public class ServiceMasterServer(ISender sender, Guid sessionId) : IServiceMasterServer
{
    private enum ServiceMasterId : byte
    {
        MessageCdb = 0,
        MessageMap = 1,
        MessagePlayerData = 2,
        MessagePlayerUpdate = 3,
        MessageMatch = 4,
        MessageHeroStats = 5,
        MessageLobbyLoadout = 6,
        MessageSearchResults = 7,
        MessageProfileData = 8,
        MessageBadges = 9,
        MessageLastPlayedHero = 10,
        MessageRegion = 11,
        MessageGetRegions = 12,
        MessagePublicKey = 13,
        MessageMatchEnded = 14,
        MessageRatingUpdate = 15,
        MessageLookingForFriends = 16,
        MessageFriend = 17,
        MessageFriendRequest = 18,
        MessageFriendSearch = 19,
        MessageFriendSearchSteam = 20,
        MessageGetLeaderboard = 21,
        MessageRegionStatusRequest = 22,
        MessageRegionStatusResponse = 23
    }
    
    private readonly IMasterServerDatabase _masterServerDatabase = Databases.MasterServerDatabase;
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<RegionStatus?>> _statusTasks = new();
    private int _currRpcId = 1;

    private ushort GetRpcId() => (ushort)Interlocked.Increment(ref _currRpcId);
    
    private static BinaryWriter CreateWriter()
    {
        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceServer);
        return writer;
    }
    
    public void SendMasterCdb(byte[]? data)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceMasterId.MessageCdb);
        writer.WriteOption(data, writer.WriteBinary);
        sender.Send(writer);
    }

    public void SendMap(string mapKey, CardMap mapCard, MapData mapData)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceMasterId.MessageMap);
        writer.Write(mapKey);
        CardMap.WriteRecord(writer, mapCard);
        MapData.WriteRecord(writer, mapData);
        sender.Send(writer);
    }

    public void SendPlayerData(PlayerData playerData)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessagePlayerData);
        PlayerData.WriteRecord(writer, playerData);
        sender.Send(writer);
    }

    public void SendPlayerUpdate(uint playerId, PlayerUpdate update)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessagePlayerUpdate);
        writer.Write(playerId);
        PlayerUpdate.WriteRecord(writer, update);
        sender.Send(writer);
    }

    public void ReceiveUsername(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var username = reader.ReadString();
        
        _masterServerDatabase.SetUsernameForPlayer(playerId, username);
    }

    public void SendMatchHistory(uint playerId, List<MatchHistoryRecord> matches)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessageMatch);
        writer.Write(playerId);
        writer.WriteList(matches, MatchHistoryRecord.WriteRecord);
        sender.Send(writer);
    }

    public void SendHeroStats(uint playerId, List<HeroStats> heroStats)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessageHeroStats);
        writer.Write(playerId);
        writer.WriteList(heroStats, HeroStats.WriteRecord);
        sender.Send(writer);
    }

    public void SendLobbyLoadout(uint playerId, Dictionary<Key, LobbyLoadout> lobbyLoadout)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessageLobbyLoadout);
        writer.Write(playerId);
        writer.WriteMap(lobbyLoadout, Key.WriteRecord, LobbyLoadout.WriteRecord);
        sender.Send(writer);
    }

    public void ReceiveLobbyLoadout(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var hero = Key.ReadRecord(reader);
        var loadout = LobbyLoadout.ReadRecord(reader);
        
        _masterServerDatabase.SetLoadoutForPlayer(playerId, hero, loadout);
    }

    public void SendSearchResults(ushort rpcId, List<SearchResult> searchResults)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessageSearchResults);
        writer.Write(rpcId);
        writer.WriteList(searchResults, SearchResult.WriteRecord);
        sender.Send(writer);
    }

    public void ReceiveSearchResults(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var pattern = reader.ReadString();

        SendSearchResults(rpcId, _masterServerDatabase.GetSearchResults(pattern).Result);
    }

    public void SendProfileData(ushort rpcId, ProfileData profileData)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessageProfileData);
        writer.Write(rpcId);
        ProfileData.WriteRecord(writer, profileData);
        sender.Send(writer);
    }

    public void ReceiveProfileData(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var playerId = reader.ReadUInt32();
        
        SendProfileData(rpcId, _masterServerDatabase.GetProfileData(playerId).Result);
    }

    public void ReceiveBadges(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var badges = reader.ReadMap<BadgeType, List<Key>, Dictionary<BadgeType, List<Key>>>(
            reader.ReadByteEnum<BadgeType>, () => reader.ReadList<Key, List<Key>>(Key.ReadRecord));

        _masterServerDatabase.SetBadgesForPlayer(playerId, badges);
    }

    public void ReceiveLastPlayedHero(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var hero = Key.ReadRecord(reader);
        
        _masterServerDatabase.SetLastPlayedForPlayer(playerId, hero);
    }

    public void ReceiveRegion(BinaryReader reader)
    {
        var host = reader.ReadString();
        var regionInfo = RegionGuiInfo.ReadRecord(reader);
        if (host == Databases.ConfigDatabase.MasterPublicHost())
        {
            _masterServerDatabase.AddRegionServer("master", host, regionInfo, this);
        }
        else
        {
            _masterServerDatabase.AddRegionServer(sessionId.ToString(), host, regionInfo, this);
            SendMasterCdb(CatalogueCache.Load());
            foreach (var map in Databases.MapDatabase.GetMapCards())
            {
                if (map.Id is null || Databases.MapDatabase.LoadMapData(Catalogue.Key(map.Id)) is not {} mapData)
                    continue;
            
                SendMap(map.Id, map, mapData);
            }
        }
        SendPublicKey(Databases.PlayerDatabase.GetPublicKey());
    }
    
    public void SendRegions(ushort rpcId, List<string> regions)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessageGetRegions);
        writer.Write(rpcId);
        writer.WriteList(regions, writer.Write);
        sender.Send(writer);
    }

    public void ReceiveRegions(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        
        SendRegions(rpcId, _masterServerDatabase.GetRegionServers().Select(r => r.Info?.Name?.Text ?? string.Empty).ToList());
    }
    
    public void SendPublicKey(string publicKey)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessagePublicKey);
        writer.Write(publicKey);
        sender.Send(writer);
    }

    public async Task<RegionStatus?> RequestStatusAsync(CancellationToken cancellationToken)
    {
        var rpcId = GetRpcId();
        var tcs = new TaskCompletionSource<RegionStatus?>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_statusTasks.TryAdd(rpcId, tcs))
        {
            throw new InvalidOperationException("Failed to register status request");
        }

        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessageRegionStatusRequest);
        writer.Write(rpcId);
        sender.Send(writer);

        using (cancellationToken.Register(() =>
               {
                   if (_statusTasks.TryRemove(rpcId, out var pending))
                   {
                       pending.TrySetCanceled(cancellationToken);
                   }
               }))
        {
            try
            {
                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                _statusTasks.TryRemove(rpcId, out _);
            }
        }
    }
    
    public void ReceiveMatchEndedForPlayer(BinaryReader reader)
    {
        var endMatchResults = EndMatchResults.ReadRecord(reader);

        _masterServerDatabase.SetNewMatchDataForPlayer(endMatchResults);
    }
    
    public void SendRatingsUpdate(Dictionary<uint, Rating> ratings)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessageRatingUpdate);
        writer.WriteMap(ratings, writer.Write, writer.Write);
        sender.Send(writer);
    }

    public void ReceiveRatingsUpdate(BinaryReader reader)
    {
        var winners = reader.ReadList<uint, List<uint>>(reader.ReadUInt32);
        var losers = reader.ReadList<uint, List<uint>>(reader.ReadUInt32);
        var excluded = reader.ReadList<uint, HashSet<uint>>(reader.ReadUInt32);
        
        _masterServerDatabase.SetNewRatings(winners, losers, excluded);
    }

    public void ReceiveRegionStatus(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var status = RegionStatus.ReadRecord(reader);

        if (_statusTasks.TryRemove(rpcId, out var tcs))
        {
            tcs.TrySetResult(status);
        }
    }
    
    public void ReceiveLookingForFriends(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var lookingForFriends = reader.ReadBoolean();
        
        _masterServerDatabase.SetLookingForFriendsForPlayer(playerId, lookingForFriends);
    }
    
    public void SendFriendUpdate(uint playerId, List<uint>? friends, List<uint>? requestsFor, List<uint>? requestsFrom)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessageFriend);
        writer.Write(playerId);
        writer.WriteOption(friends, list => writer.WriteList(list, writer.Write));
        writer.WriteOption(requestsFor, list => writer.WriteList(list, writer.Write));
        writer.WriteOption(requestsFrom, list => writer.WriteList(list, writer.Write));
        sender.Send(writer);
    }

    public void ReceiveFriendUpdate(BinaryReader reader)
    {
        var receiverId = reader.ReadUInt32();
        var senderId = reader.ReadUInt32();
        var accepted = reader.ReadBoolean();
        
        _masterServerDatabase.SetFriends(receiverId, senderId, accepted);
    }

    public void ReceiveFriendRequest(BinaryReader reader)
    {
        var receiverId = reader.ReadUInt32();
        var senderId = reader.ReadUInt32();
        
        _masterServerDatabase.SetFriendRequest(receiverId, senderId);
    }

    public void ReceiveFriendSearch(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var players = reader.ReadList<uint, List<uint>>(reader.ReadUInt32);

        SendSearchResults(rpcId, _masterServerDatabase.GetSearchResults(players).Result);
    }
    
    public void ReceiveFriendSearchSteam(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var players = reader.ReadList<ulong, List<ulong>>(reader.ReadUInt64);

        SendSearchResults(rpcId, _masterServerDatabase.GetSearchResults(players).Result);
    }
    
    public void SendLeaderboard(ushort rpcId, List<LeagueLeaderboardRecord> leagueLeaderboard)
    {
        using var writer = CreateWriter();
        writer.Write((byte)ServiceMasterId.MessageGetLeaderboard);
        writer.Write(rpcId);
        writer.WriteList(leagueLeaderboard, item => LeagueLeaderboardRecord.WriteRecord(writer, item));
        sender.Send(writer);
    }

    public void ReceiveLeaderboard(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        
        SendLeaderboard(rpcId, _masterServerDatabase.GetLeaderboard().Result);
    }

    public bool Receive(BinaryReader reader)
    {
        var serviceMasterId = reader.ReadByte();
        ServiceMasterId? masterEnum = null;
        if (Enum.IsDefined(typeof(ServiceMasterId), serviceMasterId))
        {
            masterEnum = (ServiceMasterId)serviceMasterId;
        }

        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"Service Master ID: {masterEnum.ToString()}");
        }

        switch (masterEnum)
        {
            case ServiceMasterId.MessagePlayerUpdate:
                ReceiveUsername(reader);
                break;
            case ServiceMasterId.MessageLobbyLoadout:
                ReceiveLobbyLoadout(reader);
                break;
            case ServiceMasterId.MessageSearchResults:
                ReceiveSearchResults(reader);
                break;
            case ServiceMasterId.MessageProfileData:
                ReceiveProfileData(reader);
                break;
            case ServiceMasterId.MessageBadges:
                ReceiveBadges(reader);
                break;
            case ServiceMasterId.MessageLastPlayedHero:
                ReceiveLastPlayedHero(reader);
                break;
            case ServiceMasterId.MessageRegion:
                ReceiveRegion(reader);
                break;
            case ServiceMasterId.MessageGetRegions:
                ReceiveRegions(reader);
                break;
            case ServiceMasterId.MessageMatchEnded:
                ReceiveMatchEndedForPlayer(reader);
                break;
            case ServiceMasterId.MessageRatingUpdate:
                ReceiveRatingsUpdate(reader);
                break;
            case ServiceMasterId.MessageLookingForFriends:
                ReceiveLookingForFriends(reader);
                break;
            case ServiceMasterId.MessageFriend:
                ReceiveFriendUpdate(reader);
                break;
            case ServiceMasterId.MessageFriendRequest:
                ReceiveFriendRequest(reader);
                break;
            case ServiceMasterId.MessageFriendSearch:
                ReceiveFriendSearch(reader);
                break;
            case ServiceMasterId.MessageFriendSearchSteam:
                ReceiveFriendSearchSteam(reader);
                break;
            case ServiceMasterId.MessageGetLeaderboard:
                ReceiveLeaderboard(reader);
                break;
            case ServiceMasterId.MessageRegionStatusResponse:
                ReceiveRegionStatus(reader);
                break;
            default:
                Console.WriteLine($"Master service received unsupported serviceId: {serviceMasterId}");
                return false;
        }
        
        return true;
    }
}
