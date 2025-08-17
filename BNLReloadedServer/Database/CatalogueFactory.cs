using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Database;

public static class CatalogueFactory
{
    private static ulong _nextGameId;
    
    public static CustomGameInfo CreateCustomGame(string name, string password, string playerName)
    {
        var customLogic = CatalogueHelper.GlobalLogic.CustomGame!;
        var maps = CatalogueHelper.MapList.Custom!;
        return new CustomGameInfo
        {
            Id = _nextGameId++,
            GameName = name,
            StarterNickname = playerName,
            Players = 0,
            MaxPlayers = 10,
            Private = password != string.Empty,
            MapInfo = new MapInfoCard
            {
               MapKey = maps[0]
            },
            BuildTime = customLogic.DefaultBuildTime,
            RespawnTimeMod = 0,
            HeroSwitch = false,
            SuperSupply = false,
            AllowBackfilling = true,
            ResourceCap = customLogic.DefaultResourceCap,
            InitResource = customLogic.DefaultInitResource,
            Status = CustomGameStatus.Preparing,
        };
    }
}