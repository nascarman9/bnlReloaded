using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;

namespace BNLReloadedServer.ProtocolHelpers;

public static class EffectHelper
{
    public static IDictionary<Key, ulong?> ToInfoDictionary(this ICollection<ConstEffectInfo> effects) =>
        effects
            .OrderBy(e => e.ExpirationTime.GetValueOrDefault(ulong.MaxValue))
            .GroupBy(eff => eff.Key)
            .ToDictionary(g => g.Key, g => g.Last().ExpirationTime);

    public static IReadOnlyCollection<T> GetEffectsOfType<T>(this IReadOnlyCollection<ConstEffectInfo> effects) =>
        effects.DistinctBy(e => e.Key).Select(e => e.Card.Effect).OfType<T>().ToList();
}