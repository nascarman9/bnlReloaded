using System.Collections.Immutable;
using BNLReloadedServer.Database;

namespace BNLReloadedServer.BaseTypes;

public record ConstEffectInfo(Key Key, ulong? TimestampEnd)
{
    public ConstEffectInfo(Key key) : this(key,
        Databases.Catalogue.GetCard<CardEffect>(key)?.Duration is { } dur
            ? (ulong)DateTimeOffset.Now.AddSeconds(dur).ToUnixTimeMilliseconds()
            : null)
    {
    }

    public ConstEffectInfo(Key key, float duration) : this(key,
        (ulong?)DateTimeOffset.Now.AddSeconds(duration).ToUnixTimeMilliseconds())
    {
    }
    
    public CardEffect Card => Databases.Catalogue.GetCard<CardEffect>(Key);

    public bool HasDuration => Card.Duration.HasValue && ExpirationTime.HasValue;

    public ulong? ExpirationTime { get; private set; } = TimestampEnd;

    public float DurationPercent => Card.Duration.HasValue && ExpirationTime.HasValue
        ? Math.Clamp(
            (float)(1.0 - (double)(ExpirationTime.Value - (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds()) /
                Card.Duration.Value), 0, 1)
        : 0.0f;
    
    public bool IsExpired => Card.Duration.HasValue && ExpirationTime.HasValue &&
                             ExpirationTime.Value < (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public void UpdateDuration(float? duration = null)
    {
        var dur = duration ?? Databases.Catalogue.GetCard<CardEffect>(Key)?.Duration;
        if (dur is null) return;

        ExpirationTime = (ulong)DateTimeOffset.Now.AddSeconds(dur.Value).ToUnixTimeMilliseconds();
    }
    
    public void UpdateDuration(ulong timeStampEnd) => ExpirationTime = timeStampEnd;

    public static ImmutableList<ConstEffectInfo> Convert(IDictionary<Key, ulong?> effects)
    {
        var constEffectInfoList = new List<ConstEffectInfo>(effects.Count);
        constEffectInfoList.AddRange(effects.Select(effect => new ConstEffectInfo(effect.Key, effect.Value)));
        return constEffectInfoList.ToImmutableList();
    }
}