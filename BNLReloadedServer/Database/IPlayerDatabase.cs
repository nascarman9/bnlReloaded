namespace BNLReloadedServer.Database;

public interface IPlayerDatabase
{
    public uint GetPlayerId(ulong steamId);
    public string GetAuthTokenForPlayer(uint playerId);
}