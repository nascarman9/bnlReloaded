using System.Collections.Concurrent;
using System;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Servers;
using Moserware.Skills;

namespace BNLReloadedServer.Service;

public class ServiceRegionServer(ISender sender) : IServiceRegionServer
{
    private enum ServiceRegionId : byte
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
        MessageStatusRequest = 22,
        MessageStatusResponse = 23
    }

    private ushort _currRpcId = 1;
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> _tasks = new();
    
    private readonly IPlayerDatabase _playerDatabase = Databases.PlayerDatabase;

    private ushort GetRpcId() => _currRpcId++;

    private static BinaryWriter CreateWriter()
    {
        var memStream = new MemoryStream();
        var writer = new BinaryWriter(memStream);
        writer.Write((byte)ServiceId.ServiceServer);
        return writer;
    }

    public void ReceiveMasterCdb(BinaryReader reader)
    {
        var cdb = reader.ReadOption(reader.ReadBinary);
        if (cdb is not null)
        {
            CatalogueCache.Save(cdb, CatalogueCache.MasterCdbPath);
        }
    }

    public void ReceiveMap(BinaryReader reader)
    {
        var mapKey = reader.ReadString();
        var mapCard = CardMap.ReadRecord(reader);
        var mapData = MapData.ReadRecord(reader);
        
        Databases.MapDatabase.SaveMap(mapKey, mapCard, mapData);
    }

    public void ReceivePlayerData(BinaryReader reader)
    {
        var data = PlayerData.ReadRecord(reader);

        _playerDatabase.AddPlayer(data);
    }

    public void SendUsername(uint playerId, string username)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessagePlayerUpdate);
        writer.Write(playerId);
        writer.Write(username);
        sender.Send(writer);
    }

    public void ReceivePlayerUpdate(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var update = PlayerUpdate.ReadRecord(reader);
        _playerDatabase.UpdatePlayer(playerId, update);
    }

    public void ReceiveMatchHistory(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var history = reader.ReadList<MatchHistoryRecord, List<MatchHistoryRecord>>(MatchHistoryRecord.ReadRecord);
        
        _playerDatabase.SetMatchHistory(playerId, history);
    }

    public void SendLastPlayedHero(uint playerId, Key lastPlayed)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageLastPlayedHero);
        writer.Write(playerId);
        Key.WriteRecord(writer, lastPlayed);
        sender.Send(writer);
    }

    public void ReceiveHeroStats(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var stats = reader.ReadList<HeroStats, List<HeroStats>>(HeroStats.ReadRecord);
        
        _playerDatabase.SetHeroStats(playerId, stats);
    }

    public void SendHeroLoadout(uint playerId, Key hero, LobbyLoadout loadout)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageLobbyLoadout);
        writer.Write(playerId);
        Key.WriteRecord(writer, hero);
        LobbyLoadout.WriteRecord(writer, loadout);
        sender.Send(writer);
    }

    public void ReceiveHeroLoadout(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var loadout =
            reader.ReadMap<Key, LobbyLoadout, Dictionary<Key, LobbyLoadout>>(Key.ReadRecord, LobbyLoadout.ReadRecord);
        
        _playerDatabase.SetLoadout(playerId, loadout);
    }

    public async Task<List<SearchResult>?> SendProfileSearchRequest(string pattern)
    {
        await using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageSearchResults);
        var rpcId = GetRpcId();
        writer.Write(rpcId);
        writer.Write(pattern);
        
        var tcs = new TaskCompletionSource<object>();
        _tasks[rpcId] = tcs;
        
        sender.Send(writer);
        return await tcs.Task as List<SearchResult>;
    }

    public void ReceiveProfileSearch(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var results = reader.ReadList<SearchResult, List<SearchResult>>(SearchResult.ReadRecord);
        
        if (_tasks.TryRemove(rpcId, out var tcs))
        {
            tcs.SetResult(results);
        }
    }

    public async Task<ProfileData?> SendProfileDataRequest(uint playerId)
    {
        await using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageProfileData);
        var rpcId = GetRpcId();
        writer.Write(rpcId);
        writer.Write(playerId);
        
        var tcs = new TaskCompletionSource<object>();
        _tasks[rpcId] = tcs;
        
        sender.Send(writer);
        return await tcs.Task as ProfileData;
    }

    public void ReceiveProfileData(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var profileData = ProfileData.ReadRecord(reader);

        if (_tasks.TryRemove(rpcId, out var tcs))
        {
            tcs.SetResult(profileData);
        }
    }

    public void SendBadges(uint playerId, Dictionary<BadgeType, List<Key>> badges)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageBadges);
        writer.Write(playerId);
        writer.WriteMap(badges, writer.WriteByteEnum, list => writer.WriteList(list, Key.WriteRecord));
        sender.Send(writer);
    }

    public void SendMatchEndedForPlayer(EndMatchResults endMatchResults)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageMatchEnded);
        EndMatchResults.WriteRecord(writer, endMatchResults);
        sender.Send(writer);
    }

    public void SendRegionInfo(string host, RegionGuiInfo regionGuiInfo)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageRegion);
        writer.Write(host);
        RegionGuiInfo.WriteRecord(writer, regionGuiInfo);
        sender.Send(writer);
    }

    public async Task<List<string>?> SendRegionRequest()
    {
        await using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageGetRegions);
        var rpcId = GetRpcId();
        writer.Write(rpcId);
        
        var tcs = new TaskCompletionSource<object>();
        _tasks[rpcId] = tcs;
        
        sender.Send(writer);
        return await tcs.Task as List<string>;
    }

    public void ReceiveRegions(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var regions = reader.ReadList<string, List<string>>(reader.ReadString);
        
        if (_tasks.TryRemove(rpcId, out var tcs))
        {
            tcs.SetResult(regions);
        }
    }

    public void ReceivePublicKey(BinaryReader reader)
    {
        var key = reader.ReadString();
        _playerDatabase.SetMasterPublicKey(key);
    }

    public void SendUpdateRatings(List<uint> winners, List<uint> losers, HashSet<uint> excluded)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageRatingUpdate);
        writer.WriteList(winners, writer.Write);
        writer.WriteList(losers, writer.Write);
        writer.WriteList(excluded, writer.Write);
        sender.Send(writer);
    }

    public void ReceiveUpdateRatings(BinaryReader reader)
    {
        var ratings = reader.ReadMap<uint, Rating, Dictionary<uint, Rating>>(reader.ReadUInt32, reader.ReadRating);
        
        _playerDatabase.SetRatings(ratings);
    }

    public void ReceiveStatusRequest(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var status = new RegionStatus
        {
            RegionId = Databases.ConfigDatabase.GetRegionInfo().Name?.Text ?? string.Empty,
            RegionName = Databases.ConfigDatabase.GetRegionInfo().Name?.Text,
            Host = Databases.ConfigDatabase.RegionPublicHost(),
            Port = 28101
        };

        try
        {
            if (Databases.RegionServerDatabase is { } regionDatabase)
            {
                status.OnlinePlayers = regionDatabase.GetOnlinePlayerCount();
                status.Queues = regionDatabase.GetQueueCounts();
                status.QueuePlayers = regionDatabase.GetQueuePlayers();
                status.CustomGames = regionDatabase.GetCustomGameStatuses();
                status.ActiveGames = regionDatabase.GetActiveGameStatuses();
            }
            else
            {
                status.Error = "region database unavailable";
            }
        }
        catch (Exception ex)
        {
            status.Error = $"status generation failed: {ex.Message}";
        }

        using var writer = CreateWriter();
        writer.Write((byte)ServiceRegionId.MessageStatusResponse);
        writer.Write(rpcId);
        RegionStatus.WriteRecord(writer, status);
        sender.Send(writer);
    }
    
    public void SendLookingForFriends(uint playerId, bool lookingForFriends)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageLookingForFriends);
        writer.Write(playerId);
        writer.Write(lookingForFriends);
        sender.Send(writer);
    }

    public void SendFriendUpdate(uint receiverId, uint senderId, bool accepted)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageFriend);
        writer.Write(receiverId);
        writer.Write(senderId);
        writer.Write(accepted);
        sender.Send(writer);
    }

    public void ReceiveFriendUpdate(BinaryReader reader)
    {
        var playerId = reader.ReadUInt32();
        var friends = reader.ReadOption(() => reader.ReadList<uint, List<uint>>(reader.ReadUInt32));
        var requestsFor = reader.ReadOption(() => reader.ReadList<uint, List<uint>>(reader.ReadUInt32));
        var requestsFrom = reader.ReadOption(() => reader.ReadList<uint, List<uint>>(reader.ReadUInt32));
        
        _playerDatabase.SetFriendsInfo(playerId, friends, requestsFor, requestsFrom);
    }

    public void SendFriendRequest(uint receiverId, uint senderId)
    {
        using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageFriendRequest);
        writer.Write(receiverId);
        writer.Write(senderId);
        sender.Send(writer);
    }

    public async Task<List<SearchResult>?> SendFriendSearchRequest(List<uint> players)
    {
        await using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageFriendSearch);
        
        var rpcId = GetRpcId();
        writer.Write(rpcId);
        writer.WriteList(players, writer.Write);
        
        var tcs = new TaskCompletionSource<object>();
        _tasks[rpcId] = tcs;
        
        sender.Send(writer);
        return await tcs.Task as List<SearchResult>;
    }

    public async Task<List<SearchResult>?> SendFriendSearchSteamRequest(List<ulong> players)
    {
        await using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageFriendSearchSteam);
        
        var rpcId = GetRpcId();
        writer.Write(rpcId);
        writer.WriteList(players, writer.Write);
        
        var tcs = new TaskCompletionSource<object>();
        _tasks[rpcId] = tcs;
        
        sender.Send(writer);
        return await tcs.Task as List<SearchResult>;
    }

    public async Task<List<LeagueLeaderboardRecord>?> SendLeagueLeaderboardRequest()
    {
        await using var writer = CreateWriter();
        writer.Write((byte) ServiceRegionId.MessageGetLeaderboard);
        
        var rpcId = GetRpcId();
        writer.Write(rpcId);
        
        var tcs = new TaskCompletionSource<object>();
        _tasks[rpcId] = tcs;
        
        sender.Send(writer);
        return await tcs.Task as List<LeagueLeaderboardRecord>;
    }
    
    public void ReceiveLeaderboard(BinaryReader reader)
    {
        var rpcId = reader.ReadUInt16();
        var results =
            reader.ReadList<LeagueLeaderboardRecord, List<LeagueLeaderboardRecord>>(LeagueLeaderboardRecord.ReadRecord);
        
        if (_tasks.TryRemove(rpcId, out var tcs))
        {
            tcs.SetResult(results);
        }
    }

    public bool Receive(BinaryReader reader)
    {
        var serviceRegionId = reader.ReadByte();
        ServiceRegionId? regionEnum = null;
        if (Enum.IsDefined(typeof(ServiceRegionId), serviceRegionId))
        {
            regionEnum = (ServiceRegionId)serviceRegionId;
        }

        if (Databases.ConfigDatabase.DebugMode())
        {
            Console.WriteLine($"Service Region ID: {regionEnum.ToString()}");
        }

        switch (regionEnum)
        {
            case ServiceRegionId.MessageCdb:
                ReceiveMasterCdb(reader);
                break;
            case ServiceRegionId.MessageMap:
                ReceiveMap(reader);
                break;
            case ServiceRegionId.MessagePlayerData:
                ReceivePlayerData(reader);
                break;
            case ServiceRegionId.MessagePlayerUpdate:
                ReceivePlayerUpdate(reader);
                break;
            case ServiceRegionId.MessageMatch:
                ReceiveMatchHistory(reader);
                break;
            case ServiceRegionId.MessageHeroStats:
                ReceiveHeroStats(reader);
                break;
            case ServiceRegionId.MessageLobbyLoadout:
                ReceiveHeroLoadout(reader);
                break;
            case ServiceRegionId.MessageSearchResults:
                ReceiveProfileSearch(reader);
                break;
            case ServiceRegionId.MessageProfileData:
                ReceiveProfileData(reader);
                break;
            case ServiceRegionId.MessageGetRegions:
                ReceiveRegions(reader);
                break;
            case ServiceRegionId.MessagePublicKey:
                ReceivePublicKey(reader);
                break;
            case ServiceRegionId.MessageRatingUpdate:
                ReceiveUpdateRatings(reader);
                break;
            case ServiceRegionId.MessageFriend:
                ReceiveFriendUpdate(reader);
                break;
            case ServiceRegionId.MessageStatusRequest:
                ReceiveStatusRequest(reader);
                break;
            case ServiceRegionId.MessageStatusResponse:
                break;
            case ServiceRegionId.MessageGetLeaderboard:
                ReceiveLeaderboard(reader);
                break;
            default:
                Console.WriteLine($"Region service received unsupported serviceId: {serviceRegionId}");
                return false;
        }
        
        return true;
    }
}
