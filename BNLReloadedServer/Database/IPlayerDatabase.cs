using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Database;

public interface IPlayerDatabase
{
    public uint GetPlayerId(ulong steamId);
    public string GetAuthTokenForPlayer(uint playerId);
    public uint? GetPlayerIdFromAuthToken(string authToken);
    public string GetPlayerName(uint playerId);
    public PlayerUpdate GetFullPlayerUpdate(uint playerId);
    public ProfileData GetPlayerProfile(uint playerId);
    public Key GetLastPlayedHero(uint playerId);
    public LobbyLoadout GetLoadoutForHero(uint playerId, Key heroKey);
    public Dictionary<Key, int> GetDeviceLevels(uint playerId);
    public List<uint> GetIgnoredUsers(uint playerId);
}