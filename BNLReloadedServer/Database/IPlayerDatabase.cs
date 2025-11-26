using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Service;
using Moserware.Skills;

namespace BNLReloadedServer.Database;

public interface IPlayerDatabase
{
    public bool AddPlayer(PlayerData player);
    public bool RemovePlayer(uint playerId);
    public uint? GetPlayerId(ulong steamId);
    public string GetAuthTokenForPlayer(uint playerId);
    public uint? GetPlayerIdFromAuthTokenMaster(string authToken);
    public uint? GetPlayerIdFromAuthTokenRegion(string authToken);
    public void SetMasterPublicKey(string publicKey);
    public string GetPublicKey();
    public void SetRegionServerService(IServiceRegionServer serviceRegionServer);
    public string GetPlayerName(uint playerId);
    public Task<PlayerData> GetPlayerData(uint playerId);
    public PlayerUpdate? GetFullPlayerUpdate(uint playerId);
    public ProfileData GetPlayerProfile(uint playerId);
    public Key GetLastPlayedHero(uint playerId);
    public LobbyLoadout GetLoadoutForHero(uint playerId, Key heroKey, bool defaultLoadout = false);
    public PlayerLobbyState GetDummyPlayerLobbyInfo(uint playerId, Key heroKey, TeamType team);
    public Dictionary<Key, int> GetDeviceLevels(uint playerId);
    public List<uint> GetIgnoredUsers(uint playerId);
    public Dictionary<CurrencyType, float> GetCurrency(uint playerId);
    public Task<List<string>?> GetRegions();
    public Task<List<SearchResult>?> GetSearchResults(string pattern);
    public void SetPlayerName(uint playerId, string name);
    public void SetLoadout(uint playerId, Dictionary<Key, LobbyLoadout> loadout);
    public void SetHeroStats(uint playerId, List<HeroStats> heroStats);
    public void SetMatchHistory(uint playerId, List<MatchHistoryRecord> matchHistory);
    public void SetRatings(Dictionary<uint, Rating> ratings);
    public void UpdatePlayer(uint playerId, PlayerUpdate update);
    public void UpdateLoadout(uint playerId, Key hero, LobbyLoadout loadout);
    public void UpdateLookingForFriends(uint playerId, bool lookingForFriends);
    public void UpdateLastPlayedHero(uint playerId, Key heroKey);
    public void UpdateBadges(uint playerId, Dictionary<BadgeType, List<Key>> badges);
    public void UpdateCurrency(uint playerId, Dictionary<CurrencyType, float> currencies);
    public void UpdateRatings(List<uint> winners, List<uint> losers, HashSet<uint> excluded);
    public void UpdateMatchStats(EndMatchResults endMatchResults);
}