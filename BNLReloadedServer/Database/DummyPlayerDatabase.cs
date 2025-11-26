using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Service;
using Moserware.Skills;

namespace BNLReloadedServer.Database;

public class DummyPlayerDatabase : IPlayerDatabase
{
    private uint _playerCounter;
    private readonly Dictionary<ulong, uint> _players = new();
    private const string TestUserName = "TestUser";
    private const string TestHero = "unit_hero_sarge_stone";

    private readonly League _testLeague = new()
    {
        Tier = 1,
        Division = 1,
        Points = 0,
        JoinedTime = default,
        LastPlayedTime = default,
        Status = null
    };

    private readonly PlayerProgression _testProgression = new()
    {
        PlayerProgress = new XpInfo
        {
            Level = 100,
            LevelXp = 0f,
            XpForNextLevel = CatalogueHelper.PlayerXpForLevel(101)
        },
        HeroesProgress = new Dictionary<Key, XpInfo>
        {
            {
                Catalogue.Key("unit_hero_sarge_stone"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 0f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_abe"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 0f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_astro"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 0f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_boxer"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 0f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_cogwheel"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 0f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_djinn"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 0f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_doc_eliza"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 0f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_engineer"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 100f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_hunter"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 100f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_kira"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 100f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_magnus"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 100f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_ninja"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 100f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_roly"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 100f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_kreepy"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 0f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            },
            {
                Catalogue.Key("unit_hero_trondson"), new XpInfo
                {
                    Level = 100,
                    LevelXp = 0f,
                    XpForNextLevel = CatalogueHelper.HeroXpForLevel(101)
                }
            }
        }
    };

    private readonly Dictionary<BadgeType, List<Key>> _testBadges = new()
    {
        { BadgeType.Border, [Catalogue.Key("badge_border_magnus_1")] },
        { BadgeType.Icon, [Catalogue.Key("badge_icon_community_representative")] },
        { BadgeType.Title, [Catalogue.Key("badge_title_hunter_the_most_dangerous_game")] }
    };

    private readonly Dictionary<CurrencyType, float> _testCurrencies = new()
    {
        { CurrencyType.Virtual, 10000f },
        { CurrencyType.Real, 1000f }
    };

    private readonly TimeTrialData _testTimeTrialData = new()
    {
        CompletedGoals = new Dictionary<Key, List<int>>(),
        BestResultTime = new Dictionary<Key, float>(),
        ResetTime = (ulong) new DateTimeOffset(DateTime.Today.AddDays(1)).ToUnixTimeMilliseconds()
    };

    private readonly List<Key> _testRotationHeroes =
    [
        Catalogue.Key("unit_hero_sarge_stone"),
        Catalogue.Key("unit_hero_hunter"),
        Catalogue.Key("unit_hero_engineer")
    ];
    
    private List<InventoryItem> GetInventory(uint playerId)
    {
        var inventory = new List<InventoryItem>();
        var deviceCards = CatalogueHelper.GetCards<CardDevice>(CardCategory.Device);
        var heroCards = CatalogueHelper.GetCards<CardUnit>(CardCategory.Unit)
            .FindAll(cardUnit => cardUnit.Data?.Type == UnitType.Player);
        var skinCards = CatalogueHelper.GetCards<CardSkin>(CardCategory.Skin);
        var perkCards = CatalogueHelper.GetCards<CardPerk>(CardCategory.Perk);
        var badgeCards = CatalogueHelper.GetCards<CardBadge>(CardCategory.Badge);
        var purchaseTime = (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds();
        inventory.AddRange(deviceCards.Select(deviceCard => new InventoryItem { Item = deviceCard.Key }).ToList());
        inventory.AddRange(heroCards.Select(heroCard => new InventoryItem { Item = heroCard.Key, PurchaseTime = purchaseTime }).ToList());
        inventory.AddRange(skinCards.Select(skinCard => new InventoryItem { Item = skinCard.Key, PurchaseTime = purchaseTime }).ToList());
        inventory.AddRange(perkCards.Select(perkCard => new InventoryItem { Item = perkCard.Key, PurchaseTime = purchaseTime }).ToList());
        inventory.AddRange(badgeCards.Select(badgeCard => new InventoryItem { Item = badgeCard.Key }).ToList());
        return inventory;
    }

    private Dictionary<Key, GameModeState> GetGameModeStates(uint playerId)
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

    private Dictionary<Key, int> GetRubbles(uint playerId)
    {
        var rubbleCards = CatalogueHelper.GetCards<CardRubble>(CardCategory.Rubble);
        return rubbleCards.ToDictionary(rubbleCard => rubbleCard.Key, rubbleCard => 0);
    }

    private Dictionary<Key, int> GetLootCrates(uint playerId)
    {
        var lootCrateCards = CatalogueHelper.GetCards<CardLootCrate>(CardCategory.LootCrate);
        return lootCrateCards.ToDictionary(lc => lc.Key, lc => 0);
    }

    public bool AddPlayer(PlayerData player) => true;

    public bool RemovePlayer(uint playerId) => true;

    public uint? GetPlayerId(ulong steamId)
    {
        if (_players.TryGetValue(steamId, out var value)) return value;
        value = Interlocked.Increment(ref _playerCounter);
        _players[steamId] = value;
        return value;
    }

    public string GetAuthTokenForPlayer(uint playerId) => playerId.ToString();

    public uint? GetPlayerIdFromAuthTokenMaster(string authToken) => uint.Parse(authToken);

    public uint? GetPlayerIdFromAuthTokenRegion(string authToken) => uint.Parse(authToken);
    public void SetMasterPublicKey(string publicKey)
    {
    }

    public string GetPublicKey() => "";
    public void SetRegionServerService(IServiceRegionServer serviceRegionServer)
    {
    }

    public string GetPlayerName(uint playerId) => TestUserName;
    
    public Task<PlayerData> GetPlayerData(uint playerId)
    {
        throw new NotImplementedException();
    }

    public PlayerUpdate? GetFullPlayerUpdate(uint playerId)
    {
        var globalLogic = CatalogueHelper.GlobalLogic;
        return new PlayerUpdate
        {
            Nickname = TestUserName,
            League = _testLeague,
            Progression = _testProgression,
            Friends = [],
            RequestsFromFriends = [],
            RequestsFromMe = [],
            Merits = globalLogic.MeritLogic.MeritInitial,
            LeaverRating = globalLogic.LeaverRating.InitValue,
            LeaverState = LeaverState.Normal,
            Notifications = new Dictionary<int, Notification>(),
            Influence = globalLogic.MeritLogic.InfluenceInitial,
            GraveyardPermanent = false,
            GraveyardLeaveTime = null,
            SelectedBadges = _testBadges,
            VoiceMute = [],
            MatchmakerBanEnd = null,
            LookingForFriends = false,
            TutorialTokens = 0,
            TutorialCompleted = false,
            Challenges = [null, null, null],
            ChallengeRefusesLeft = 1,
            ChallengeDayEndTime = (ulong) new DateTimeOffset(DateTime.Today.AddDays(1)).ToUnixTimeMilliseconds(),
            ChallengesCompleted = 3,
            Currency = _testCurrencies,
            Inventory = GetInventory(playerId),
            OneTimeRewards = [],
            DailyMatchPlayed = true,
            DailyWinAvailable = false,
            FullMatchesPlayed = 1,
            TimeTrial = _testTimeTrialData,
            GameModeStates = GetGameModeStates(playerId),
            IsInSquadFinder = false,
            SquadFinderSettings = new SquadFinderSettings
            {
                GameModes = [],
                Locales = [],
                Heroes = []
            },
            SquadFinderPlayers = [],
            DeviceLevels = GetDeviceLevels(playerId),
            Rubbles = GetRubbles(playerId),
            NextLootCrateTime = (int) DateTimeOffset.Now.AddHours(4).ToUnixTimeSeconds(),
            LootCrates = GetLootCrates(playerId),
            LastPlayedHero = GetLastPlayedHero(playerId),
            NewItems = [],
            HeroesOnRotation = _testRotationHeroes
        };
    }

    public ProfileData GetPlayerProfile(uint playerId)
    {
        var steamId = _players.Select(kv => (kv.Key, kv.Value)).First(kv => kv.Value == playerId).Key;
        return new ProfileData
        {
            Nickname = TestUserName,
            SteamId = steamId,
            League = _testLeague,
            Progression = _testProgression,
            MatchHistory = [],
            HeroStats = [],
            GlobalStats = new GlobalStats(),
            SelectedBadges = _testBadges,
            LookingForFriends = false,
            FriendsCount = 0
        };
    }

    public Key GetLastPlayedHero(uint playerId) => Catalogue.Key(TestHero);

    public LobbyLoadout GetLoadoutForHero(uint playerId, Key heroKey, bool defaultLoadout = false)
    {
        var heroData = (UnitDataPlayer) Databases.Catalogue.GetCard<CardUnit>(heroKey)?.Data;
        if (defaultLoadout)
        {
            return new LobbyLoadout
            {
                HeroKey = heroKey,
                Devices = CatalogueHelper.GetDefaultDevices(heroKey),
                Perks = [],
                SkinKey = heroData.Skins[0]
            };
        }
        
        return new LobbyLoadout
        {
            HeroKey = heroKey,
            Devices = CatalogueHelper.GetDefaultDevices(heroKey),
            Perks = [],
            SkinKey = heroData.Skins[0]
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
            var dCard = Databases.Catalogue.GetCard<CardDevice>(deviceCard.Devices[0]);
            deviceLevels[deviceCard.Key] = dCard.DeviceLevels.Count;
        }
        return deviceLevels;
    }

    public List<uint> GetIgnoredUsers(uint playerId) => [];

    public Dictionary<CurrencyType, float> GetCurrency(uint playerId) => _testCurrencies;
    public async Task<List<string>?> GetRegions()
    {
        return [];
    }

    public async Task<List<SearchResult>?> GetSearchResults(string pattern)
    {
        return [];
    }

    public void SetPlayerName(uint playerId, string name)
    {
    }

    public void SetLoadout(uint playerId, Dictionary<Key, LobbyLoadout> loadout)
    {
    }

    public void SetHeroStats(uint playerId, List<HeroStats> heroStats)
    {
    }

    public void SetMatchHistory(uint playerId, List<MatchHistoryRecord> matchHistory)
    {
    }

    public void SetRatings(Dictionary<uint, Rating> ratings)
    {
    }

    public void UpdatePlayer(uint playerId, PlayerUpdate update)
    {
    }

    public void UpdateLoadout(uint playerId, Key hero, LobbyLoadout loadout)
    {
    }

    public void UpdateLookingForFriends(uint playerId, bool lookingForFriends)
    {
    }

    public void UpdateLastPlayedHero(uint playerId, Key heroKey)
    {
    }

    public void UpdateBadges(uint playerId, Dictionary<BadgeType, List<Key>> badges)
    {
    }

    public void UpdateCurrency(uint playerId, Dictionary<CurrencyType, float> currencies)
    {
    }

    public void UpdateRatings(List<uint> winners, List<uint> losers, HashSet<uint> excluded)
    {
    }

    public void UpdateMatchStats(EndMatchResults endMatchResults)
    {
    }
}