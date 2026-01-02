using System.Collections.Concurrent;
using System.Text;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Service;
using Moserware.Skills;
using SQLite;

namespace BNLReloadedServer.Database;

public class MasterServerDatabase : IMasterServerDatabase
{
    private readonly List<RegionInfo> _regionServers = [];
    private readonly ConcurrentDictionary<string, IServiceMasterServer> _regionServerConnections = new();
    private readonly SQLiteAsyncConnection _playerDb;
    
    private readonly SemaphoreSlim _asyncLock = new(1, 1);

    public MasterServerDatabase()
    {
        _playerDb = new SQLiteAsyncConnection(Databases.PlayerDatabaseFile);
        _playerDb.CreateTableAsync<PlayerRecord>().Wait();
    }

    public List<RegionInfo> GetRegionServers() => _regionServers;

    public IServiceMasterServer? GetRegionServerService(string id) =>
        _regionServerConnections.TryGetValue(id, out var service) ? service : null;

    public bool AddRegionServer(string id, string host, RegionGuiInfo regionGuiInfo, IServiceMasterServer? serviceMasterServer = null)
    {
        if (_regionServers.Any(x => x.Id == id)) return false;
        _regionServers.Add(new RegionInfo
        {
            Id = id,
            Host = host,
            Info = regionGuiInfo,
            Port = 28101
        });
        
        if (serviceMasterServer != null)
            _regionServerConnections[id] = serviceMasterServer;
        
        return true;
    }

    public bool RemoveRegionServer(string id)
    {
        if (_regionServerConnections.Remove(id, out _))
        {
            return _regionServers.RemoveAll(r => r.Id == id) > 0;
        }
        
        return false;
    }

    public RegionInfo? GetRegionServer(string id) => _regionServers.FirstOrDefault(x => x.Id == id);

    public async Task<PlayerData> AddPlayer(ulong steamId, string playerName, string region)
    {
        var newRecord = new PlayerRecord
        {
            SteamId = steamId,
            Username = playerName,
            PlayerRole = PlayerRole.User,
            Region = region,
            LeagueInfo = null,
            Progression = PlayerProgression.WriteByteRecord(CatalogueHelper.GetDefaultProgression()),
            LookingForFriends = false,
            BadgeInfo = PlayerData.WriteBadgeByteRecord(new Dictionary<BadgeType, List<Key>>()),
            LoadoutData = PlayerData.WriteLoadoutByteRecord(new Dictionary<Key, LobbyLoadout>()),
            HeroStats = PlayerData.WriteStatByteRecord([]),
            MatchHistory = PlayerData.WriteMatchByteRecord([]),
            TimeTrialInfo = TimeTrialData.WriteByteRecord(new TimeTrialData
            {
                BestResultTime = new Dictionary<Key, float>(),
                CompletedGoals = new Dictionary<Key, List<int>>(),
                ResetTime = 0
            })
        };

        var players = await _playerDb.Table<PlayerRecord>().Where(x => x.SteamId == newRecord.SteamId).ToListAsync();
        if (players is { Count: > 0 })
        {
            return PlayerData.FromPlayerRecord(players.First());
        }
        
        await _playerDb.InsertAsync(newRecord);
        return PlayerData.FromPlayerRecord(newRecord);
    }
    
    public async Task<PlayerData?> GetPlayer(ulong steamId)
    {
        var record = await _playerDb.Table<PlayerRecord>().Where(x => x.SteamId == steamId).FirstOrDefaultAsync();
        return record != null ? PlayerData.FromPlayerRecord(record) : null;
    }

    public async Task<PlayerData?> GetPlayer(uint playerId)
    {
        var record = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == playerId).FirstOrDefaultAsync();
        return record != null ? PlayerData.FromPlayerRecord(record) : null;
    }

    public async Task<bool> SetRegionForPlayer(uint playerId, string region)
    {
        await _asyncLock.WaitAsync();
        try
        {
            var record = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == playerId).FirstOrDefaultAsync();
            if (record == null) return false;
            record.Region = region;
            await _playerDb.UpdateAsync(record);
        }
        finally
        {
            _asyncLock.Release();
        }
        return true;
    }

    public async Task<bool> SetUsernameForPlayer(uint playerId, string username)
    {
        await _asyncLock.WaitAsync();
        try
        {
            var record = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == playerId).FirstOrDefaultAsync();
            if (record == null) return false;
            record.Username = username;
            await _playerDb.UpdateAsync(record);
            foreach (var regionServer in _regionServerConnections.Values)
            {
                regionServer.SendPlayerUpdate(playerId, new PlayerUpdate
                {
                    Nickname = username
                });
            }
        }
        finally
        {
            _asyncLock.Release();
        }
        return true;
    }

    public async Task<bool> SetLookingForFriendsForPlayer(uint playerId, bool lookingForFriends)
    {
        await _asyncLock.WaitAsync();
        try
        {
            var record = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == playerId).FirstOrDefaultAsync();
            if (record == null) return false;
            record.LookingForFriends = lookingForFriends;
            
            await _playerDb.UpdateAsync(record);
            foreach (var regionServer in _regionServerConnections.Values)
            {
                regionServer.SendPlayerUpdate(playerId, new PlayerUpdate
                {
                    LookingForFriends = lookingForFriends
                });
            }
        }
        finally
        {
            _asyncLock.Release();
        }

        return true;
    }

    public async Task<bool> SetLastPlayedForPlayer(uint playerId, Key hero)
    {
        await _asyncLock.WaitAsync();
        try
        {
            var record = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == playerId).FirstOrDefaultAsync();
            if (record == null) return false;
            record.LastPlayedHero = hero.GetCard<CardUnit>()?.Id;
            await _playerDb.UpdateAsync(record);

            foreach (var regionServer in _regionServerConnections.Values)
            {
                regionServer.SendPlayerUpdate(playerId, new PlayerUpdate
                {
                    LastPlayedHero = hero
                });
            }
        }
        finally
        {
            _asyncLock.Release();
        }

        return true;
    }

    public async Task<bool> SetBadgesForPlayer(uint playerId, Dictionary<BadgeType, List<Key>> badges)
    {
        await _asyncLock.WaitAsync();
        try
        {
            var record = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == playerId).FirstOrDefaultAsync();
            if (record == null) return false;
            record.BadgeInfo = PlayerData.WriteBadgeByteRecord(badges);
            await _playerDb.UpdateAsync(record);

            foreach (var regionServer in _regionServerConnections.Values)
            {
                regionServer.SendPlayerUpdate(playerId, new PlayerUpdate
                {
                    SelectedBadges = badges
                });
            }
        }
        finally
        {
            _asyncLock.Release();
        }

        return true;
    }

    public async Task<bool> SetLoadoutForPlayer(uint playerId, Key hero, LobbyLoadout loadout)
    {
        await _asyncLock.WaitAsync();
        try
        {
            var record = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == playerId).FirstOrDefaultAsync();
            if (record == null) return false;

            var pData = PlayerData.FromPlayerRecord(record);
            pData.HeroLoadouts[hero] = loadout;
            var newLoadouts = pData.HeroLoadouts;
            record = pData.ToPlayerRecord();

            await _playerDb.UpdateAsync(record);
            foreach (var regionServer in _regionServerConnections.Values)
            {
                regionServer.SendLobbyLoadout(playerId, newLoadouts);
            }
        }
        finally
        {
            _asyncLock.Release();
        }

        return true;
    }

    public async Task<bool> SetNewMatchDataForPlayer(EndMatchResults endMatchResults)
    {
        await _asyncLock.WaitAsync();
        try
        {
            var record = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == endMatchResults.PlayerId)
                .FirstOrDefaultAsync();
            if (record == null) return false;

            // Convert record to player data
            var currPlayer = PlayerData.FromPlayerRecord(record);

            // Update hero stats
            var endMatchData = endMatchResults.MatchData;
            var hero = endMatchData.HeroKey;
            
            if (currPlayer.HeroStats.All(x => x.Hero != hero))
            {
                currPlayer.HeroStats.Add(new HeroStats
                {
                    Hero = hero,
                    TotalMatches = 0,
                    Wins = 0
                });
            }
            
            foreach (var stat in currPlayer.HeroStats.FindAll(s => s.Hero == hero))
            {
                if (endMatchData is not { IsBackfiller: false }) continue;

                stat.TotalMatches += 1;
                if (endMatchData is { IsWinner: true })
                {
                    stat.Wins += 1;
                }
            }

            // Update progression
            if (endMatchData.OldPlayerXp is not null && endMatchData.RewardXp > 0)
            {
                currPlayer.Progression.PlayerProgress =
                    CatalogueHelper.LeveLUp(endMatchData.OldPlayerXp, endMatchData.RewardXp);
            }

            if (endMatchData.NewHeroXp is not null)
            {
                currPlayer.Progression.HeroesProgress?[hero] = endMatchData.NewHeroXp;
            }

            // Create match history
            var encoder = new UTF8Encoding();
            var currPlayerResults =
                endMatchData.PlayersData?.FirstOrDefault(x => x.PlayerId == endMatchResults.PlayerId);
            var history = new MatchHistoryRecord
            {
                MatchId = encoder.GetBytes(endMatchResults.GameInstanceId),
                HeroKey = hero,
                SkinKey = endMatchData.SkinKey,
                MapKey = endMatchResults.MapKey,
                GameModeKey = endMatchResults.GameModeKey,
                MatchEndTime = endMatchResults.MatchEndTime,
                MatchSeconds = endMatchData.MatchSeconds,
                IsWinner = endMatchData.IsWinner,
                IsBackfiller = endMatchData.IsBackfiller,
                IsQuit = endMatchData.IsAfk,
                ResourcesEarned = currPlayerResults?.Stats?.Stats?.GetValueOrDefault(PlayerMatchStatType.Earned) ?? 0,
                BlocksBuilt = currPlayerResults?.Stats?.Stats?.GetValueOrDefault(PlayerMatchStatType.Built) ?? 0,
                BlockAssist = currPlayerResults?.Stats?.Stats?.GetValueOrDefault(PlayerMatchStatType.BlockAssist) ?? 0,
                Destruction = currPlayerResults?.Stats?.Stats?.GetValueOrDefault(PlayerMatchStatType.Destroyed) ?? 0,
                ObjectiveDamage =
                    currPlayerResults?.Stats?.Stats?.GetValueOrDefault(PlayerMatchStatType.Objective) ?? 0,
                Kill = currPlayerResults?.Stats?.Stats?.GetValueOrDefault(PlayerMatchStatType.Kill) ?? 0,
                Death = currPlayerResults?.Stats?.Stats?.GetValueOrDefault(PlayerMatchStatType.Death) ?? 0,
                Assist = currPlayerResults?.Stats?.Stats?.GetValueOrDefault(PlayerMatchStatType.Assist) ?? 0
            };

            currPlayer.MatchHistory = currPlayer.MatchHistory.Prepend(history).Take(10).ToList();

            record = currPlayer.ToPlayerRecord();
            await _playerDb.UpdateAsync(record);
            foreach (var regionServer in _regionServerConnections.Values)
            {
                regionServer.SendHeroStats(currPlayer.PlayerId, currPlayer.HeroStats);
                regionServer.SendPlayerUpdate(currPlayer.PlayerId, new PlayerUpdate
                {
                    Progression = currPlayer.Progression
                });
                regionServer.SendMatchHistory(currPlayer.PlayerId, currPlayer.MatchHistory);
            }
        }
        finally
        {
            _asyncLock.Release();
        }

        return true;
    }

    public async Task<bool> SetNewRatings(List<uint> winners, List<uint> losers, HashSet<uint> excluded)
    {
        await _asyncLock.WaitAsync();
        try
        {
            // If there's barely anyone left, just don't bother
            if (winners.Count <= 2 && losers.Count <= 2)
            {
                return true;
            }
            
            var winnerRecords =
                await _playerDb.Table<PlayerRecord>().Where(x => winners.Contains(x.PlayerId)).ToListAsync() ?? [];
            var loserRecords =
                await _playerDb.Table<PlayerRecord>().Where(x => losers.Contains(x.PlayerId)).ToListAsync() ?? [];

            var winnerPlayers = winnerRecords.Select(PlayerData.FromPlayerRecord).ToList();
            var loserPlayers = loserRecords.Select(PlayerData.FromPlayerRecord).ToList();

            var winnerRatings = winnerPlayers.ToDictionary(k => new Player<uint>(k.PlayerId), v => v.Rating);
            var loserRatings = loserPlayers.ToDictionary(k => new Player<uint>(k.PlayerId), v => v.Rating);

            var newRatings =
                TrueSkillCalculator.CalculateNewRatings(Databases.DefaultGameInfo, [winnerRatings, loserRatings], 1, 2);

            var allPlayers = winnerPlayers.Union(loserPlayers).ToList();
            foreach (var (playerId, rating) in newRatings.ToDictionary(k => k.Key.Id, v => v.Value))
            {
                if (excluded.Contains(playerId))
                {
                    continue;
                }

                var player = allPlayers.FirstOrDefault(x => x.PlayerId == playerId);
                player?.Rating = rating;
            }

            var newRecords = allPlayers.Select(d => d.ToPlayerRecord());
            await _playerDb.UpdateAllAsync(newRecords);
            foreach (var regionServer in _regionServerConnections.Values)
            {
                regionServer.SendRatingsUpdate(allPlayers.ToDictionary(p => p.PlayerId, p => p.Rating));
            }
        }
        finally
        {
            _asyncLock.Release();
        }
        
        return true;
    }

    public async Task<bool> SetFriends(uint receiverId, uint senderId, bool accepted)
    {
        await _asyncLock.WaitAsync();
        try
        {
            var recordReceiver = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == receiverId)
                .FirstOrDefaultAsync();
            var recordSender = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == senderId)
                .FirstOrDefaultAsync();
            if (recordReceiver == null || recordSender == null) return false;
            
            var receiverPlayer = PlayerData.FromPlayerRecord(recordReceiver);
            var senderPlayer = PlayerData.FromPlayerRecord(recordSender);

            receiverPlayer.RequestsFromFriends.Remove(senderId);
            senderPlayer.RequestsFromMe.Remove(receiverId);

            if (accepted)
            {
                if (!receiverPlayer.Friends.Contains(senderId))
                    receiverPlayer.Friends.Add(senderId);
                if (!senderPlayer.Friends.Contains(receiverId))
                    senderPlayer.Friends.Add(receiverId);
            }
            else
            {
                receiverPlayer.Friends.Remove(senderId);
                senderPlayer.Friends.Remove(receiverId);
            }
            
            recordReceiver = receiverPlayer.ToPlayerRecord();
            recordSender = senderPlayer.ToPlayerRecord();
            List<PlayerRecord> recList = [recordReceiver, recordSender];
            await _playerDb.UpdateAllAsync(recList);
            foreach (var regionServer in _regionServerConnections.Values)
            {
                regionServer.SendFriendUpdate(receiverId, receiverPlayer.Friends, receiverPlayer.RequestsFromFriends, null);
                regionServer.SendFriendUpdate(senderId, senderPlayer.Friends, null, senderPlayer.RequestsFromMe);
            }
        }
        finally
        {
            _asyncLock.Release();
        }
        
        return true;
    }

    public async Task<bool> SetFriendRequest(uint receiverId, uint senderId)
    {
        await _asyncLock.WaitAsync();
        try
        {
            var recordReceiver = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == receiverId)
                .FirstOrDefaultAsync();
            var recordSender = await _playerDb.Table<PlayerRecord>().Where(x => x.PlayerId == senderId)
                .FirstOrDefaultAsync();
            if (recordReceiver == null || recordSender == null) return false;
            
            var receiverPlayer = PlayerData.FromPlayerRecord(recordReceiver);
            var senderPlayer = PlayerData.FromPlayerRecord(recordSender);
            
            if (!receiverPlayer.RequestsFromFriends.Contains(senderId))
                receiverPlayer.RequestsFromFriends.Add(senderId);
            if (!senderPlayer.RequestsFromMe.Contains(receiverId))
                senderPlayer.RequestsFromMe.Add(receiverId);
            
            recordReceiver = receiverPlayer.ToPlayerRecord();
            recordSender = senderPlayer.ToPlayerRecord();
            List<PlayerRecord> recList = [recordReceiver, recordSender];
            await _playerDb.UpdateAllAsync(recList);
            foreach (var regionServer in _regionServerConnections.Values)
            {
                regionServer.SendFriendUpdate(receiverId, null, receiverPlayer.RequestsFromFriends, null);
                regionServer.SendFriendUpdate(senderId, null, null, senderPlayer.RequestsFromMe);
            }
        }
        finally
        {
            _asyncLock.Release();
        }
        
        return true;
    }

    public void HaveRegionLoadPlayer(string regionServer, PlayerData playerData)
    {
        if (regionServer == "master")
        {
            Databases.PlayerDatabase.AddPlayer(playerData);
            return;
        }
        
        if (_regionServerConnections.TryGetValue(regionServer, out var connection))
        {
            connection.SendPlayerData(playerData);
        }
    }

    public async Task<ProfileData> GetProfileData(uint playerId)
    {
        var player = await GetPlayer(playerId);
        if (player != null)
        {
            return new ProfileData
            {
                Nickname = player.Nickname,
                SteamId = player.SteamId,
                League = player.League,
                Progression = player.Progression,
                MatchHistory = player.MatchHistory,
                HeroStats = player.HeroStats,
                GlobalStats = new GlobalStats(),
                SelectedBadges = player.Badges,
                LookingForFriends = player.LookingForFriends,
                FriendsCount = player.Friends.Count
            };
        }
        
        return new ProfileData
        {
            MatchHistory = [],
            HeroStats = [],
            LookingForFriends = false,
            FriendsCount = 0
        };
    }

    public async Task<List<SearchResult>> GetSearchResults(string pattern)
    {
        var records = await _playerDb.Table<PlayerRecord>().Where(x => x.Username.StartsWith(pattern)).ToListAsync() ?? [];
        return records.Select(rec => new SearchResult
        {
            PlayerId = rec.PlayerId,
            SteamId = rec.SteamId,
            Nickname = rec.Username
        }).ToList();
    }

    public async Task<List<SearchResult>> GetSearchResults(List<uint> playerIds)
    {
        var records = await _playerDb.Table<PlayerRecord>().Where(x => playerIds.Contains(x.PlayerId)).ToListAsync() ?? [];
        return records.Select(rec => new SearchResult
        {
            PlayerId = rec.PlayerId,
            SteamId = rec.SteamId,
            Nickname = rec.Username
        }).ToList();
    }

    public async Task<List<SearchResult>> GetSearchResults(List<ulong> steamIds)
    {
        var records = await _playerDb.Table<PlayerRecord>().Where(x => steamIds.Contains(x.SteamId)).ToListAsync() ?? [];
        return records.Select(rec => new SearchResult
        {
            PlayerId = rec.PlayerId,
            SteamId = rec.SteamId,
            Nickname = rec.Username
        }).ToList();
    }

    public async Task<List<LeagueLeaderboardRecord>> GetLeaderboard()
    {
        var records = await _playerDb.Table<PlayerRecord>().Where(p =>
                p.RatingMean != Databases.DefaultMean || p.RatingDeviation != Databases.DefaultSd)
            .OrderByDescending(p => p.RatingMean).Take(100)
            .ToListAsync();

        return records.Select(PlayerData.FromPlayerRecord).Select((p, idx) => new LeagueLeaderboardRecord
        {
            PlayerId = p.PlayerId,
            SteamId = p.SteamId,
            PlayerName = p.Nickname,
            Points = (int)double.Ceiling(p.Rating.Mean * 100),
            Status = idx + 1,
            Wins = p.HeroStats.Sum(h => h.Wins),
            TotalMatches = p.HeroStats.Sum(h => h.TotalMatches),
            RegistrationTime = default,
            Region = p.Region
        }).ToList();
    }
}
