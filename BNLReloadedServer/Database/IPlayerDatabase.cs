using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Database;

public interface IPlayerDatabase
{
    public uint GetPlayerId(ulong steamId);
    public string GetAuthTokenForPlayer(uint playerId);
    public uint? GetPlayerIdFromAuthToken(string authToken);
    public ProfileData GetPlayerProfile(uint playerId);
}