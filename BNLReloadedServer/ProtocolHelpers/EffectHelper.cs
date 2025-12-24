using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class EffectHelper
{
    extension(IEnumerable<ConstEffectInfo> effects)
    {
        public IDictionary<Key, ulong?> ToInfoDictionary() =>
            effects
                .OrderBy(e => e.ExpirationTime.GetValueOrDefault(ulong.MaxValue))
                .GroupBy(eff => eff.Key)
                .ToDictionary(g => g.Key, g => g.Last().ExpirationTime);

        public IEnumerable<T> GetEffectsOfType<T>() =>
            effects.DistinctBy(e => e.Key).Select(e => e.Card.Effect).OfType<T>();
    }
}