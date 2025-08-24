using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class MapInfoHelper
{
    public static Key GetKey(this MapInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        return (info as MapInfoCard)?.MapKey ?? throw new ArgumentException("Map info is not a card", nameof (info));
    }
}