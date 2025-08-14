using BNLReloadedServer.Database;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectInfo(Key key, ulong? timestampEnd)
{
    public Key Key = key;
    public ulong? TimestampEnd = timestampEnd;

    public CardEffect Card => Databases.Catalogue.GetCard<CardEffect>(Key)!;

    public bool HasDuration => Card.Duration.HasValue && TimestampEnd.HasValue;

    public float DurationPercent => Card.Duration.HasValue && TimestampEnd.HasValue ? Math.Clamp((float) (1.0 - (double) 0 / Card.Duration.Value), 0, 1) : 0.0f;

    public static List<ConstEffectInfo> Convert(Dictionary<Key, ulong?> effects)
    {
        var constEffectInfoList = new List<ConstEffectInfo>(effects.Count);
        constEffectInfoList.AddRange(effects.Select(effect => new ConstEffectInfo(effect.Key, effect.Value)));
        return constEffectInfoList;
    }
}