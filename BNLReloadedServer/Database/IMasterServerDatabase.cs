using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Service;
using Moserware.Skills;

namespace BNLReloadedServer.Database;

public interface IMasterServerDatabase
{
    public List<RegionInfo> GetRegionServers();
    public bool AddRegionServer(string id, string host, RegionGuiInfo regionGuiInfo, IServiceMasterServer? serviceMasterServer = null);
    public bool RemoveRegionServer(string id);
    public RegionInfo? GetRegionServer(string id);
    public Task<PlayerData> AddPlayer(ulong steamId, string playerName, string region);
    public Task<PlayerData?> GetPlayer(ulong steamId);
    public Task<PlayerData?> GetPlayer(uint playerId);
    public Task<bool> SetRegionForPlayer(uint playerId, string region);
    public Task<bool> SetUsernameForPlayer(uint playerId, string username);
    public Task<bool> SetLookingForFriendsForPlayer(uint playerId, bool lookingForFriends);
    public Task<bool> SetLastPlayedForPlayer(uint playerId, Key hero);
    public Task<bool> SetBadgesForPlayer(uint playerId, Dictionary<BadgeType, List<Key>> badges);
    public Task<bool> SetLoadoutForPlayer(uint playerId, Key hero, LobbyLoadout loadout);
    public Task<bool> SetNewMatchDataForPlayer(EndMatchResults endMatchResults);
    public Task<bool> SetNewRatings(List<uint> winners, List<uint> losers, HashSet<uint> excluded);
    public void HaveRegionLoadPlayer(string regionServer, PlayerData playerData);
    public Task<ProfileData> GetProfileData(uint playerId);
    public Task<List<SearchResult>> GetSearchResults(string pattern);
}