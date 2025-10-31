namespace BNLReloadedServer.BaseTypes;

public partial class Unit
{
    public Dictionary<ScoreType, float>? Stats { get; }

    public void UpdateStat(ScoreType scoreType, float amount)
    {
        if (amount <= 0 || Stats is null) return;
        if (!Stats.TryAdd(scoreType, amount))
        {
            Stats[scoreType] += amount;
        }

        switch (scoreType)
        {
            case ScoreType.Kills:
                _updater.UpdateMatchStats(this, kills: (int)MathF.Round(amount));
                break;
            case ScoreType.Deaths:
                _updater.UpdateMatchStats(this, deaths: (int)MathF.Round(amount));
                break;
            case ScoreType.Assists:
                _updater.UpdateMatchStats(this, assists: (int)MathF.Round(amount));
                break;
        }
    }
}