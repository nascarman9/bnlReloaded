using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public class KeyEqualityComparer : IEqualityComparer<Key>
{
    public static readonly KeyEqualityComparer Instance = new();

    public bool Equals(Key x, Key y) => x == y;

    public int GetHashCode(Key obj) => obj.GetHashCode();
}