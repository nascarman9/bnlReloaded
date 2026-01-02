using System.Threading;
using System.Threading.Tasks;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using Moserware.Skills;

namespace BNLReloadedServer.Service;

public interface IServiceMasterServer : IService
{
    public void SendMasterCdb(byte[]? data);
    public void SendMap(string mapKey, CardMap mapCard, MapData mapData);
    public void SendPlayerData(PlayerData playerData);
    public void SendPlayerUpdate(uint playerId, PlayerUpdate update);
    public void SendMatchHistory(uint playerId, List<MatchHistoryRecord> matches);
    public void SendHeroStats(uint playerId, List<HeroStats> heroStats);
    public void SendLobbyLoadout(uint playerId, Dictionary<Key, LobbyLoadout> lobbyLoadout);
    public void SendSearchResults(ushort rpcId, List<SearchResult> searchResults);
    public void SendProfileData(ushort rpcId, ProfileData profileData);
    public void SendRegions(ushort rpcId, List<string> regions);
    public void SendPublicKey(string publicKey);
    public void SendRatingsUpdate(Dictionary<uint, Rating> ratings);
    public void SendFriendUpdate(uint playerId, List<uint>? friends, List<uint>? requestsFor, List<uint>? requestsFrom);
    public void SendLeaderboard(ushort rpcId, List<LeagueLeaderboardRecord> leagueLeaderboard);
    public Task<RegionStatus?> RequestStatusAsync(CancellationToken cancellationToken);
}
