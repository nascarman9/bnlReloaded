using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServiceRegionServer : IService
{
    public void SendUsername(uint playerId, string username);
    public void SendLastPlayedHero(uint playerId, Key lastPlayed);
    public void SendHeroLoadout(uint playerId, Key hero, LobbyLoadout loadout);
    public Task<List<SearchResult>?> SendProfileSearchRequest(string pattern);
    public Task<ProfileData?> SendProfileDataRequest(uint playerId);
    public void SendBadges(uint playerId, Dictionary<BadgeType, List<Key>> badges);
    public void SendMatchEndedForPlayer(EndMatchResults endMatchResults);
    public void SendRegionInfo(string host, RegionGuiInfo regionGuiInfo);
    public Task<List<string>?> SendRegionRequest();
    public void SendUpdateRatings(List<uint> winners, List<uint> losers, HashSet<uint> excluded);
    public void SendLookingForFriends(uint playerId, bool lookingForFriends);
    public void SendFriendUpdate(uint receiverId, uint senderId, bool accepted);
    public void SendFriendRequest(uint receiverId, uint senderId);
    public Task<List<SearchResult>?> SendFriendSearchRequest(List<uint> players);
    public Task<List<SearchResult>?> SendFriendSearchSteamRequest(List<ulong> players);
    public Task<List<LeagueLeaderboardRecord>?> SendLeagueLeaderboardRequest();
}