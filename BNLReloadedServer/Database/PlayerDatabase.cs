using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Service;
using Moserware.Skills;

namespace BNLReloadedServer.Database;

public class PlayerDatabase : IPlayerDatabase
{
    private record PlayerToken(uint PlayerId, DateTimeOffset Timestamp);
    
    private readonly ConcurrentDictionary<uint, PlayerData> _players = new();
    private readonly ConcurrentDictionary<uint, List<ulong>> _steamFriends = new();
    
    private readonly ConcurrentDictionary<uint, TaskCompletionSource<PlayerData>> _playerTasks = new();
    
    private readonly RSA _tokenSigner = new RSACryptoServiceProvider();
    
    private string? _rsaMasterPublicKey;
    
    private IServiceRegionServer _serviceRegionServer;
    
    private readonly Dictionary<CurrencyType, float> _testCurrencies = new()
    {
        { CurrencyType.Virtual, 10000f },
        { CurrencyType.Real, 1000f }
    };
    
    private List<InventoryItem> GetInventory(uint playerId)
    {
        if (!_players.TryGetValue(playerId, out var player))
        {
            return [];
        }
        
        var globalLogic = CatalogueHelper.GlobalLogic;
        
        var inventory = new List<InventoryItem>();
        var deviceCards = CatalogueHelper.GetCards<CardDevice>(CardCategory.Device);
        var heroCards = CatalogueHelper.GetHeroes().Select(h => h.GetCard<CardUnit>()).OfType<CardUnit>(); 
        var skinCards = CatalogueHelper.GetCards<CardSkin>(CardCategory.Skin);

        var offPerks = globalLogic.Perks?.Offensive?.Select(p => p.GetCard<CardPerk>()).OfType<CardPerk>() ?? [];
        var defPerks = globalLogic.Perks?.Defensive?.Select(p => p.GetCard<CardPerk>()).OfType<CardPerk>() ?? [];
        var heroPerks = globalLogic.Perks?.Heroes?.SelectMany(p => p.Value.Select(perk => perk.GetCard<CardPerk>())).OfType<CardPerk>() ?? [];

        var perkCards = heroPerks.Union(offPerks.Union(defPerks));
        var badgeCards = globalLogic.AvailableBadges?.Select(b => b.GetCard<CardBadge>()).OfType<CardBadge>() ?? [];

        if (player.Role is not PlayerRole.Core)
        {
            badgeCards = badgeCards.Where(b => b.Id != "badge_icon_community_representative");
        }
        
        var purchaseTime = (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds();
        inventory.AddRange(deviceCards.Select(deviceCard => new InventoryItem { Item = deviceCard.Key }).ToList());
        inventory.AddRange(heroCards.Select(heroCard => new InventoryItem { Item = heroCard.Key, PurchaseTime = purchaseTime }).ToList());
        inventory.AddRange(skinCards.Select(skinCard => new InventoryItem { Item = skinCard.Key, PurchaseTime = purchaseTime }).ToList());
        inventory.AddRange(perkCards.Select(perkCard => new InventoryItem { Item = perkCard.Key, PurchaseTime = purchaseTime }).ToList());
        inventory.AddRange(badgeCards.Select(badgeCard => new InventoryItem { Item = badgeCard.Key }).ToList());
        return inventory;
    }
    
    private static Dictionary<Key, GameModeState> GetGameModeStates()
    {
        var gameModeCards = CatalogueHelper.GetCards<CardGameMode>(CardCategory.GameMode);
        var result = new Dictionary<Key, GameModeState>();
        foreach (var gameModeCard in gameModeCards)
        {
            var gameModeState = new GameModeState
            {
                IsAvailable = gameModeCard != CatalogueHelper.GetMode(GameRankingType.Graveyard),
                NextToggleTime = null
            };
            result.Add(gameModeCard.Key, gameModeState);
        }
        return result;
    }
    
    private static Dictionary<Key, int> GetRubbles()
    {
        var rubbleCards = CatalogueHelper.GetCards<CardRubble>(CardCategory.Rubble);
        return rubbleCards.ToDictionary(rubbleCard => rubbleCard.Key, _ => 0);
    }

    private static Dictionary<Key, int> GetLootCrates()
    {
        var lootCrateCards = CatalogueHelper.GetCards<CardLootCrate>(CardCategory.LootCrate);
        return lootCrateCards.ToDictionary(lc => lc.Key, _ => 0);
    }

    public bool AddPlayer(PlayerData player)
    {
        _players[player.PlayerId] = player;
        if (_playerTasks.Remove(player.PlayerId, out var task))
        {
            task.SetResult(player);
        }

        return true;
    }

    public bool RemovePlayer(uint playerId)
    {
        _steamFriends.Remove(playerId, out _);
        return _players.Remove(playerId, out _);
    }

    public uint? GetPlayerId(ulong steamId) => _players.Values.FirstOrDefault(p => p.SteamId == steamId)?.PlayerId;
    
    public string GetAuthTokenForPlayer(uint playerId)
    {
        var tokenRecord = new PlayerToken(playerId, DateTimeOffset.UtcNow);
        var token = JsonSerializer.Serialize(tokenRecord);
        var encoder = new UTF8Encoding();
        var signature = _tokenSigner.SignData(encoder.GetBytes(token), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var sigString = Convert.ToBase64String(signature);
        token += "#" + sigString;
        return token;
    }

    public uint? GetPlayerIdFromAuthTokenMaster(string authToken)
    {
        var sigStart = authToken.IndexOf('#');
        if (sigStart < 0)
        {
            return null;
        }
        
        var token = authToken[..sigStart];
        var signature = authToken[(sigStart + 1)..];
        var encoder = new UTF8Encoding();
        var encodedBytes = encoder.GetBytes(token);
        var signatureBytes = Convert.FromBase64String(signature);
        
        if (Databases.ConfigDatabase.IsMaster())
        {
            if (!_tokenSigner.VerifyData(encodedBytes, signatureBytes, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1)) return null;
        }
        else
        {
            using var rsa = new RSACryptoServiceProvider();
            try
            {
                rsa.ImportFromPem(_rsaMasterPublicKey);
                if (!rsa.VerifyData(encodedBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                    return null;
            }
            finally
            {
                rsa.PersistKeyInCsp = false;
            }
        }
        
        var player = JsonSerializer.Deserialize<PlayerToken>(encodedBytes);
        return player?.Timestamp.AddMinutes(10) < DateTimeOffset.UtcNow ? null : player?.PlayerId;
    }

    public uint? GetPlayerIdFromAuthTokenRegion(string authToken)
    {
        var sigStart = authToken.IndexOf('#');
        if (sigStart < 0)
        {
            return null;
        }
        
        var token = authToken[..sigStart];
        var signature = authToken[(sigStart + 1)..];
        var encoder = new UTF8Encoding();
        var encodedBytes = encoder.GetBytes(token);
        var signatureBytes = Convert.FromBase64String(signature);

        if (!_tokenSigner.VerifyData(encodedBytes, signatureBytes, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1)) return null;
        
        var player = JsonSerializer.Deserialize<PlayerToken>(encodedBytes);
        return player?.Timestamp.AddMinutes(10) < DateTimeOffset.UtcNow ? null : player?.PlayerId;
    }

    public void SetMasterPublicKey(string publicKey)
    {
        _rsaMasterPublicKey = publicKey;
    }

    public string GetPublicKey() => _tokenSigner.ExportRSAPublicKeyPem();
    
    public void SetRegionServerService(IServiceRegionServer serviceRegionServer)
    {
        _serviceRegionServer = serviceRegionServer;
    }

    public string GetPlayerName(uint playerId) => _players.GetValueOrDefault(playerId)?.Nickname ?? string.Empty;
    
    public async Task<PlayerData> GetPlayerData(uint playerId)
    {
        var tcs = new TaskCompletionSource<PlayerData>();
        
        _playerTasks.TryAdd(playerId, tcs);
        if (!_players.TryGetValue(playerId, out var playerData)) return await tcs.Task;
        
        _playerTasks.Remove(playerId, out _);
        return playerData;
    }

    public PlayerData? GetPlayerDataNoWait(uint playerId) => _players.GetValueOrDefault(playerId);

    public PlayerUpdate? GetFullPlayerUpdate(uint playerId)
    {
        if (!_players.TryGetValue(playerId, out var player))
        {
            return null;
        }
        
        var globalLogic = CatalogueHelper.GlobalLogic;
        var rewardsLogic = CatalogueHelper.RewardsLogic;
        var friends = GetFriends(playerId).Result;
        var requestsFor = GetFriendRequestsFor(playerId).Result;
        var requestsFrom = GetFriendRequestsFrom(playerId).Result;
        return new PlayerUpdate
        {
            Nickname = player.Nickname,
            League = player.League,
            Progression = player.Progression,
            Friends = friends,
            RequestsFromFriends = requestsFor,
            RequestsFromMe = requestsFrom,
            Merits = globalLogic.MeritLogic?.MeritInitial ?? 25f,
            LeaverRating = globalLogic.LeaverRating?.InitValue ?? 0,
            LeaverState = LeaverState.Normal,
            Notifications = player.Notifications,
            Influence = globalLogic.MeritLogic?.InfluenceInitial ?? 0.34f,
            GraveyardPermanent = player.GraveyardPermanent,
            GraveyardLeaveTime = player.GraveyardLeaveTime,
            SelectedBadges = player.Badges,
            VoiceMute = [],
            MatchmakerBanEnd = player.MatchmakerBanEnd,
            LookingForFriends = player.LookingForFriends,
            TutorialTokens = player.TutorialTokens,
            TutorialCompleted = true,
            Challenges = [null, null, null],
            ChallengeRefusesLeft = 1,
            ChallengeDayEndTime = (ulong) new DateTimeOffset(DateTime.Today.AddDays(1)).ToUnixTimeMilliseconds(),
            ChallengesCompleted = 3,
            Currency = _testCurrencies,
            Inventory = GetInventory(playerId),
            OneTimeRewards =
                [rewardsLogic.CompleteFirstMatch, rewardsLogic.CompleteSecondMatch, rewardsLogic.PlayTutorialVideo],
            DailyMatchPlayed = true,
            DailyWinAvailable = false,
            FullMatchesPlayed = player.HeroStats.Sum(h => h.TotalMatches),
            TimeTrial = player.TimeTrial,
            GameModeStates = GetGameModeStates(),
            IsInSquadFinder = false,
            SquadFinderSettings = new SquadFinderSettings
            {
                GameModes = [],
                Locales = [],
                Heroes = []
            },
            SquadFinderPlayers = [],
            DeviceLevels = GetDeviceLevels(playerId),
            Rubbles = GetRubbles(),
            NextLootCrateTime = (int) DateTimeOffset.Now.AddHours(4).ToUnixTimeSeconds(),
            LootCrates = GetLootCrates(),
            LastPlayedHero = GetLastPlayedHero(playerId),
            NewItems = [],
            HeroesOnRotation = []
        };
    }

    public ProfileData GetPlayerProfile(uint playerId)
    {
        if (!_players.TryGetValue(playerId, out var player))
        {
            var profile = _serviceRegionServer.SendProfileDataRequest(playerId).Result;
            if (profile != null)
            {
                return profile;
            }
            
            return new ProfileData
            {
                MatchHistory = [],
                HeroStats = [],
                LookingForFriends = false,
                FriendsCount = 0
            };
        }
        
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

    public Key GetLastPlayedHero(uint playerId)
    {
        if (!_players.TryGetValue(playerId, out var player) || player.LastPlayedHero == null)
        {
            return CatalogueHelper.GetHeroes().First();
        }

        return player.LastPlayedHero.Value;
    }

    public LobbyLoadout GetLoadoutForHero(uint playerId, Key heroKey, bool defaultLoadout = false)
    {
        var heroData = Databases.Catalogue.GetCard<CardUnit>(heroKey)?.Data as UnitDataPlayer;
        if (defaultLoadout || !_players.TryGetValue(playerId, out var player) ||
            !player.HeroLoadouts.TryGetValue(heroKey, out var loadout) || loadout.HeroKey == Key.None ||
            loadout.SkinKey == Key.None || loadout.Devices is null || loadout.Perks is null)
        {
            return new LobbyLoadout
            {
                HeroKey = heroKey,
                Devices = CatalogueHelper.GetDefaultDevices(heroKey),
                Perks = [],
                SkinKey = heroData?.Skins?[0] ?? Key.None
            };
        }

        return new LobbyLoadout
        {
            HeroKey = loadout.HeroKey,
            Devices = loadout.Devices?.ToDictionary(),
            Perks = loadout.Perks?.ToList(),
            SkinKey = loadout.SkinKey
        };
    }

    public PlayerLobbyState GetDummyPlayerLobbyInfo(uint playerId, Key heroKey, TeamType team)
    {
        var profileData = Databases.PlayerDatabase.GetPlayerProfile(playerId);
        var defaultLoadout = GetLoadoutForHero(playerId, heroKey, true);
        var deviceLevels = Databases.PlayerDatabase.GetDeviceLevels(playerId);
        return new PlayerLobbyState
        {
            PlayerId = playerId,
            SteamId = profileData.SteamId,
            Nickname = profileData.Nickname,
            SquadId = null,
            Role = PlayerRoleType.None,
            PlayerLevel = profileData.Progression?.PlayerProgress?.Level ?? 1,
            SelectedBadges = profileData.SelectedBadges,
            Team = team,
            Hero = heroKey,
            Devices = defaultLoadout.Devices,
            Perks = defaultLoadout.Perks,
            RestrictedHeroes = [],
            SkinKey = defaultLoadout.SkinKey,
            Ready = true,
            CanLoadout = true,
            Status = LobbyStatus.Online,
            LookingForFriends = profileData.LookingForFriends,
            DeviceLevels = deviceLevels
        };
    }

    public Dictionary<Key, int> GetDeviceLevels(uint playerId)
    {
        var deviceLevels = new Dictionary<Key, int>();
        foreach (var deviceCard in CatalogueHelper.GetCards<CardDeviceGroup>(CardCategory.DeviceGroup))
        {
            var dCard = deviceCard.Devices?[0].GetCard<CardDevice>();
            deviceLevels[deviceCard.Key] = dCard?.DeviceLevels?.Count ?? 1;
        }
        return deviceLevels;
    }

    public List<uint> GetIgnoredUsers(uint playerId) => [];

    public Dictionary<CurrencyType, float> GetCurrency(uint playerId) => _testCurrencies;
    
    public async Task<List<string>?> GetRegions() => await _serviceRegionServer.SendRegionRequest();

    public async Task<List<SearchResult>?> GetSearchResults(string pattern) =>
        await _serviceRegionServer.SendProfileSearchRequest(pattern);
    
    public async Task<List<FriendInfo>> GetFriends(uint playerId)
    {
        if (!_players.TryGetValue(playerId, out var player))
        {
            return [];
        }
        
        var friends = new List<FriendInfo>();
        var friendInfo = await _serviceRegionServer.SendFriendSearchRequest(player.Friends);
        if (friendInfo == null) return friends;
        foreach (var friend in friendInfo)
        {
            var online = _players.TryGetValue(friend.PlayerId, out var friendData);
            friends.Add(new FriendInfo
            {
                PlayerId = friend.PlayerId,
                SteamId = friend.SteamId,
                Nickname = friend.Nickname,
                Online = online,
                InMainMenu = online && Databases.RegionServerDatabase.GetLastScene(friend.PlayerId) is SceneMainMenu,
                Region = friendData?.Region
            });
        }

        if (!_steamFriends.TryGetValue(playerId, out var steamFriends)) return friends;
        
        var steamInfo = await _serviceRegionServer.SendFriendSearchSteamRequest(steamFriends);
        if (steamInfo == null) return friends;
        foreach (var friend in steamInfo)
        {
            var online = _players.TryGetValue(friend.PlayerId, out var friendData);
            if (friends.All(f => f.PlayerId != friend.PlayerId))
            {
                friends.Add(new FriendInfo
                {
                    PlayerId = friend.PlayerId,
                    SteamId = friend.SteamId,
                    Nickname = friend.Nickname,
                    Online = online,
                    InMainMenu = online && Databases.RegionServerDatabase.GetLastScene(friend.PlayerId) is SceneMainMenu,
                    Region = friendData?.Region
                });
            }
        }

        return friends;
    }

    public async Task<List<FriendRequest>> GetFriendRequestsFor(uint playerId)
    {
        if (!_players.TryGetValue(playerId, out var player))
        {
            return [];
        }
        
        var friendsRequests = new List<FriendRequest>();
        var friendInfo = await _serviceRegionServer.SendFriendSearchRequest(player.RequestsFromFriends);
        if (friendInfo == null) return friendsRequests;
        friendsRequests.AddRange(friendInfo.Select(friend => new FriendRequest
            { PlayerId = friend.PlayerId, SteamId = friend.SteamId, Nickname = friend.Nickname }));

        return friendsRequests;
    }
    
    public async Task<List<FriendRequest>> GetFriendRequestsFrom(uint playerId)
    {
        if (!_players.TryGetValue(playerId, out var player))
        {
            return [];
        }
        
        var friendsRequests = new List<FriendRequest>();
        var friendInfo = await _serviceRegionServer.SendFriendSearchRequest(player.RequestsFromMe);
        if (friendInfo == null) return friendsRequests;
        friendsRequests.AddRange(friendInfo.Select(friend => new FriendRequest
            { PlayerId = friend.PlayerId, SteamId = friend.SteamId, Nickname = friend.Nickname }));

        return friendsRequests;
    }

    public async Task<List<LeagueLeaderboardRecord>?> GetLeaderboard() =>
        await _serviceRegionServer.SendLeagueLeaderboardRequest();

    public bool IsBanned(uint playerId)
    {
        if (_players.TryGetValue(playerId, out var player))
        {
            return player.MatchmakerBanEnd is not null &&
                   DateTimeOffset.FromUnixTimeMilliseconds((long)player.MatchmakerBanEnd.Value) > DateTimeOffset.Now;
        }
        
        return false;
    }

    public void SetPlayerName(uint playerId, string name)
    {
        if (_players.TryGetValue(playerId, out var player))
        {
            player.Nickname = name;
            Databases.RegionServerDatabase.UpdateChatName(playerId, name);
        }
        
        _serviceRegionServer.SendUsername(playerId, name);
    }

    public void SetLoadout(uint playerId, Dictionary<Key, LobbyLoadout> loadout)
    {
        if (_players.TryGetValue(playerId, out var player))
        {
            player.HeroLoadouts = loadout;
        }
    }

    public void SetHeroStats(uint playerId, List<HeroStats> heroStats)
    {
        if (_players.TryGetValue(playerId, out var player))
        {
            player.HeroStats = heroStats;
        }
    }

    public void SetMatchHistory(uint playerId, List<MatchHistoryRecord> matchHistory)
    {
        if (_players.TryGetValue(playerId, out var player))
        {
            player.MatchHistory = matchHistory;
        }
    }

    public void SetRatings(Dictionary<uint, Rating> ratings)
    {
        foreach (var (playerId, rating) in ratings)
        {
            if (_players.TryGetValue(playerId, out var player))
            {
                player.Rating = rating;
            }
        }
    }

    public void SetFriendsInfo(uint playerId, List<uint>? friends, List<uint>? requestsFor, List<uint>? requestsFrom)
    {
        if (!_players.TryGetValue(playerId, out var player))
        {
            return;
        }

        if (friends != null)
        {
            player.Friends = friends;
            Databases.RegionServerDatabase.NotifyFriends(playerId);
        }

        if (requestsFor != null)
        {
            player.RequestsFromFriends = requestsFor;
        }

        if (requestsFrom != null)
        {
            player.RequestsFromMe = requestsFrom;
        }
    }

    public void SetSteamFriends(uint playerId, List<ulong> steamFriends)
    {
        _steamFriends[playerId] = steamFriends;
    }

    public void UpdatePlayer(uint playerId, PlayerUpdate update)
    {
        if (!_players.TryGetValue(playerId, out var player))
        {
            return;
        }
        
        if (update.Progression != null)
        {
            player.Progression = update.Progression;
        }

        if (update.Nickname != null)
        {
            player.Nickname = update.Nickname;
        }

        if (update.Friends != null)
        {
            player.Friends = update.Friends.Select(p => p.PlayerId).ToList();
        }

        if (update.LookingForFriends.HasValue)
        {
            player.LookingForFriends = update.LookingForFriends.Value;
        }

        if (update.GraveyardLeaveTime.HasValue)
        {
            player.GraveyardLeaveTime = update.GraveyardLeaveTime.Value;
        }

        if (update.GraveyardPermanent.HasValue)
        {
            player.GraveyardPermanent = update.GraveyardPermanent.Value;
        }

        if (update.LastPlayedHero.HasValue)
        {
            player.LastPlayedHero = update.LastPlayedHero.Value;
        }

        if (update.MatchmakerBanEnd.HasValue)
        {
            player.MatchmakerBanEnd = update.MatchmakerBanEnd.Value;
        }

        if (update.Notifications != null)
        {
            player.Notifications = update.Notifications;
        }

        if (update.SelectedBadges != null)
        {
            player.Badges = update.SelectedBadges;
        }

        if (update.TimeTrial != null)
        {
            player.TimeTrial = update.TimeTrial;
        }

        if (update.TutorialTokens.HasValue)
        {
            player.TutorialTokens = update.TutorialTokens.Value;
        }

        if (update.League != null)
        {
            player.League = update.League;
        }

        if (update.RequestsFromFriends != null)
        {
            player.RequestsFromFriends = update.RequestsFromFriends.Select(p => p.PlayerId).ToList();
        }

        if (update.RequestsFromMe != null)
        {
            player.RequestsFromMe = update.RequestsFromMe.Select(p => p.PlayerId).ToList();
        }
    }

    public void UpdateLoadout(uint playerId, Key hero, LobbyLoadout loadout) =>
        _serviceRegionServer.SendHeroLoadout(playerId, hero, loadout);

    public void UpdateLookingForFriends(uint playerId, bool lookingForFriends) => 
        _serviceRegionServer.SendLookingForFriends(playerId, lookingForFriends);

    public void UpdateLastPlayedHero(uint playerId, Key heroKey) => 
        _serviceRegionServer.SendLastPlayedHero(playerId, heroKey);

    public void UpdateBadges(uint playerId, Dictionary<BadgeType, List<Key>> badges) => 
        _serviceRegionServer.SendBadges(playerId, badges);

    public void UpdateCurrency(uint playerId, Dictionary<CurrencyType, float> currencies)
    {
    }

    public void UpdateRatings(List<uint> winners, List<uint> losers, HashSet<uint> excluded) =>
        _serviceRegionServer.SendUpdateRatings(winners, losers, excluded);

    public void UpdateMatchStats(EndMatchResults endMatchResults) =>
        _serviceRegionServer.SendMatchEndedForPlayer(endMatchResults);

    public void UpdateFriends(uint receiverId, uint senderId, bool accepted) =>
        _serviceRegionServer.SendFriendUpdate(receiverId, senderId, accepted);

    public void UpdateFriendRequest(uint receiverId, uint senderId) =>
        _serviceRegionServer.SendFriendRequest(receiverId, senderId);
}