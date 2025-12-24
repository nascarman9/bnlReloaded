using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ServerTypes;

public class BlockIntervalUpdater(BlockSpecialInsideEffect effect, BlockSource source)
{
    private readonly Dictionary<Unit, DateTimeOffset?> _intervals = new();
    public readonly BlockSpecialInsideEffect Effect = effect;

    public int Count => _intervals.Count;

    public bool AddUnit(Unit unit)
    {
        if (_intervals.ContainsKey(unit)) return false;
        
        _intervals.Add(unit, Effect.Interval.HasValue ? DateTimeOffset.Now : null);
        return true;
    }

    public bool RemoveUnit(Unit unit)
    {
        if (Effect.InsideEffects is { Count: > 0 } effects)
        {
            unit.RemoveEffects(effects.Select(eff => new ConstEffectInfo(eff, null)), source.Team, source);
        }
        
        return _intervals.Remove(unit);
    }

    public void Clear()
    {
        foreach (var unit in _intervals.Keys.ToList())
        {
            RemoveUnit(unit);
        }
    }

    public IReadOnlyCollection<Unit> GetApplyIntervalTo()
    {
        var result = _intervals.Where(i => i.Value.HasValue && i.Value < DateTimeOffset.Now).Select(i => i.Key)
            .ToArray();
        
        foreach (var (unit, time) in _intervals)
        {
            if (Effect.Interval.HasValue && time is not null && time < DateTimeOffset.Now)
            {
                _intervals[unit] = time.Value.AddSeconds(Effect.Interval.Value);
            }
        }
        
        return result;
    }
}