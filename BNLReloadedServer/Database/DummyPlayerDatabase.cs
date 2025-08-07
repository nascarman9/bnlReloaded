namespace BNLReloadedServer.Database;

public class DummyPlayerDatabase : IPlayerDatabase
{
    public uint GetPlayerId(ulong steamId)
    {
        return 1;
    }

    public string GetAuthTokenForPlayer(uint playerId)
    {
        return playerId.GetHashCode().ToString();
    }
}