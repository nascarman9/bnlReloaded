using System.Timers;
using Timer = System.Timers.Timer;

namespace BNLReloadedServer.BaseTypes;

public partial class Unit
{
    private void RunInterval(float interval,
        List<InstEffect> intervalEffects,
        Func<bool> constEffectCheck,
        Func<Unit[]> getAffectedUnits,
        Action<IEnumerable<Unit>, InstEffect> onApplyInstEffect)
    {
        DoCheck();
        
        var timer = new Timer(TimeSpan.FromSeconds(interval));
        timer.Elapsed += OnIntervalElapsed;
        timer.AutoReset = true;
        timer.Start();
        return;

        void OnIntervalElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_updater.EnqueueAction(() =>
                {
                    if (!constEffectCheck())
                    {
                        timer.Stop();
                        timer.Dispose();
                        return;
                    }

                    DoCheck();
                })) return;
            
            timer.Stop();
            timer.Dispose();
        }

        void DoCheck()
        {
            var affectedUnits = getAffectedUnits();
            foreach (var effect in intervalEffects)
            {
                onApplyInstEffect(affectedUnits, effect);
            }
        }
    }

    private void RunShower(UnitDataShower shower, Action<Random> onShowerEffect)
    {
        var rand = new Random();
        var timer = new Timer(TimeSpan.FromSeconds(shower.InitDelay));
        timer.Elapsed += OnIntervalElapsed;
        timer.AutoReset = true;
        timer.Start();
        return;

        void OnIntervalElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_updater.EnqueueAction(() =>
                {
                    if (IsDead)
                    {
                        timer.Stop();
                        timer.Dispose();
                        return;
                    }

                    timer.Interval = DoCheck();
                })) return;
            
            timer.Stop();
            timer.Dispose();
        }

        float DoCheck()
        {
            onShowerEffect(rand);
            return shower.HitDelay + (rand.NextSingle() * 2 - 1) * shower.HitDelayRandom;
        }
    }
}