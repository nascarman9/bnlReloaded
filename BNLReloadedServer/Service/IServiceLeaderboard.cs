using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Service;

public interface IServiceLeaderboard : IService
{
    public void SendGetTimeTrialLeaderboard(ushort rpcId, Dictionary<Key, List<TtLeaderboardRecord>>? data,
        ELeaderboardUpdateCooldown? eLeaderboardUpdateCooldown = null, string? error = null);
    public void SendGetLeagueLeaderboard(ushort rpcId, List<LeagueLeaderboardRecord>? data, 
        ELeaderboardUpdateCooldown? eLeaderboardUpdateCooldown = null, string? error = null);
}