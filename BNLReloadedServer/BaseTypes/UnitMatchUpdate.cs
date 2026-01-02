using System.Diagnostics;
using System.Numerics;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public partial class Unit
{
    public Dictionary<ScoreType, float>? Stats { get; }

    private const float AssistSeconds = 30;

    public readonly Stopwatch TimeRespawning = new();
    public readonly Stopwatch TimeSpotted = new();
    public readonly Stopwatch TimeControlled = new();
    public readonly Stopwatch TimeAtMaxHp = new();
    public readonly Stopwatch TimeAtLowHp = new();
    public readonly Stopwatch TimeStoppedMoving = new();

    private void UpdateStat(ScoreType scoreType, float amount)
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
            case ScoreType.BlocksBuiltResource:
            case ScoreType.HeroBlocksBuiltResource:
            case ScoreType.WorldBuiltResource:
            case ScoreType.DevicesBuiltResource:
                _updater.UpdateMatchStats(this, blocksBuilt: (int)MathF.Floor(amount));
                break;
            case ScoreType.BlocksBuilt:
            case ScoreType.HeroBlocksBuilt:
            case ScoreType.WorldBuilt:
            case ScoreType.DevicesBuilt:
                break;
            case ScoreType.DevicesDestroyedResource:
                _updater.UpdateMatchStats(this, blocksDestroyed: (int)MathF.Floor(amount));
                break;
            case ScoreType.BlocksDestroyedResource:
            case ScoreType.HeroBlocksDestroyedResource:
            case ScoreType.WorldDestroyedResource:
            case ScoreType.BlocksDestroyed:
            case ScoreType.HeroBlocksDestroyed:
            case ScoreType.WorldDestroyed:
            case ScoreType.DevicesDestroyed:
                break;
            case ScoreType.ResourceEarnedTotal:
                _updater.UpdateMatchStats(this, resourcesEarned: amount);
                break;
        }
    }
    
    public void BuiltBlock(DeviceType deviceType, float resources)
    {
        switch (deviceType)
        {
            case DeviceType.None:
            case DeviceType.World:
                UpdateStat(ScoreType.WorldBuilt, 1);
                UpdateStat(ScoreType.WorldBuiltResource, resources);
                break;
            case DeviceType.Block:
                UpdateStat(ScoreType.BlocksBuilt, 1);
                UpdateStat(ScoreType.BlocksBuiltResource, resources);
                break;
            case DeviceType.Device:
                UpdateStat(ScoreType.DevicesBuilt, 1);
                UpdateStat(ScoreType.DevicesBuiltResource, resources);
                break;
            case DeviceType.Hero:
                UpdateStat(ScoreType.HeroBlocksBuilt, 1);
                UpdateStat(ScoreType.HeroBlocksBuiltResource, resources);
                break;
        }
    }

    public static float ResolveDeviceResourceValue(IInternalDevice? deviceCard, float? enemyReward, float? playerReward)
    {
        if (deviceCard?.BaseCost is { } baseCost)
        {
            return baseCost;
        }

        if (enemyReward is { } enemyValue)
        {
            return enemyValue;
        }

        return playerReward ?? 0;
    }

    public void DestroyedBlock(DeviceType deviceType, float resources)
    {
        switch (deviceType)
        {
            case DeviceType.None:
            case DeviceType.World:
                UpdateStat(ScoreType.WorldDestroyed, 1);
                UpdateStat(ScoreType.WorldDestroyedResource, resources);
                break;
            case DeviceType.Block:
                UpdateStat(ScoreType.BlocksDestroyed, 1);
                UpdateStat(ScoreType.BlocksDestroyedResource, resources);
                break;
            case DeviceType.Device:
                UpdateStat(ScoreType.DevicesDestroyed, 1);
                UpdateStat(ScoreType.DevicesDestroyedResource, resources);
                break;
            case DeviceType.Hero:
                UpdateStat(ScoreType.HeroBlocksDestroyed, 1);
                UpdateStat(ScoreType.HeroBlocksDestroyedResource, resources);
                break;
        }
    }

    public void EarnedResource(float resources, bool isMining)
    {
        UpdateStat(ScoreType.ResourceEarnedTotal, resources);
        if (isMining)
        {
            UpdateStat(ScoreType.ResourceEarnedMining, resources);
        }
    }

    public void Healed(float amount, EffectSource? healSource, Unit? healerPlayer)
    {
        switch (UnitCard?.Health?.Health?.HealthType)
        {
            case HealthType.Player when PlayerId is not null:
                if (healSource is UnitSource { Unit.PlayerId: not null })
                {
                    UpdateStat(ScoreType.HealedByHero, amount);
                    healerPlayer?.UpdateStat(ScoreType.HealPlayerByHero, amount);
                }
                else
                {
                    UpdateStat(ScoreType.HealedByBlock, amount);
                    healerPlayer?.UpdateStat(ScoreType.HealPlayerByBlock, amount);
                }
                    
                break;
            
            case HealthType.World:
            case HealthType.Objective:
                healerPlayer?.UpdateStat(
                    healSource is UnitSource { Unit.PlayerId: not null }
                        ? ScoreType.RepairByHero
                        : ScoreType.RepairByBlock, amount);
                break;
        }
    }

    public void RepairedBlock(float amount, EffectSource? healSource) =>
        UpdateStat(healSource is UnitSource { Unit.PlayerId: not null }
            ? ScoreType.RepairByHero
            : ScoreType.RepairByBlock, amount);

    public void DamageStatsUpdate(TeamType targetTeam, float damage, bool crit, bool isFall, Unit? attacker, Unit? attackerPlayer)
    {
        if (isFall)
        {
            if (PlayerId is not null)
            {
                UpdateStat(ScoreType.FallHitDamage, damage);
            }
            return;
        }
        
        switch (UnitCard?.Health?.Health?.HealthType)
        {
            case HealthType.Player when PlayerId is not null:
                if (attackerPlayer is not null && targetTeam != attackerPlayer.Team)
                {
                    RecentDamagers[attackerPlayer] = DateTimeOffset.Now.AddSeconds(AssistSeconds);
                    
                    var playerData = PlayerUnitData;
                    if (playerData?.Class == CatalogueHelper.BrawnClassKey)
                    {
                        attackerPlayer.UpdateStat(ScoreType.DamageBrawn, damage);
                    }
                    else if (playerData?.Class == CatalogueHelper.SkillsClassKey)
                    {
                        attackerPlayer.UpdateStat(ScoreType.DamageSkills, damage);
                    }
                    else if (playerData?.Class == CatalogueHelper.BrainsClassKey)
                    {
                        attackerPlayer.UpdateStat(ScoreType.DamageBrains, damage);
                    }
                }

                if (attackerPlayer is not null && attackerPlayer == attacker)
                {
                    UpdateStat(ScoreType.DamagedByHero, damage);
                    if (crit)
                    {
                        UpdateStat(ScoreType.DamagedCriticalByHero, damage);
                    }

                    if (targetTeam != attackerPlayer.Team)
                    {
                        attackerPlayer.UpdateStat(ScoreType.DamagePlayerByHero, damage);
                        if (crit)
                        {
                            attackerPlayer.UpdateStat(ScoreType.DamagePlayerCriticalByHero, damage);
                        }
                    }
                }
                else if (attacker?.UnitCard?.DeviceType is not DeviceType.None)
                {
                    UpdateStat(ScoreType.DamagedByBlock, damage);
                    attackerPlayer?.UpdateStat(ScoreType.DamagePlayerByBlock, damage);
                }
                else
                {
                    UpdateStat(ScoreType.DamagedByOther, damage);
                }
                
                if (attackerPlayer?.Team == targetTeam)
                {
                    UpdateStat(ScoreType.DamagedByFriendlyPlayer, damage);
                    attackerPlayer.UpdateStat(ScoreType.DamageFriendlyPlayer, damage);
                }
                
                var attackerData = attackerPlayer?.PlayerUnitData;
                if (attackerData?.Class == CatalogueHelper.BrawnClassKey)
                {
                    UpdateStat(ScoreType.DamagedByBrawn, damage);
                }
                else if (attackerData?.Class == CatalogueHelper.SkillsClassKey)
                {
                    UpdateStat(ScoreType.DamagedBySkills, damage);
                }
                else if (attackerData?.Class == CatalogueHelper.BrainsClassKey)
                {
                    UpdateStat(ScoreType.DamagedByBrains, damage);
                }
                break;
            
            case HealthType.Objective when UnitCard?.IsObjective is true && attackerPlayer is not null:
                attackerPlayer.UpdateStat(UnitCard.IsBase ? ScoreType.DamageBase : ScoreType.DamageShield, damage);
                break;
        }
    }
    
    public void KillStatsUpdate(TeamType targetTeam, bool crit, Unit? killer, Unit? killerPlayer, IEnumerable<Unit> assisters)
    {
        switch (UnitCard?.Health?.Health?.HealthType)
        {
            case HealthType.Player when PlayerId is not null:
                if (killerPlayer is not null && targetTeam != killerPlayer.Team)
                {
                    killerPlayer.UpdateStat(ScoreType.Kills, 1);
            
                    var playerData = PlayerUnitData;
                    if (playerData?.Class == CatalogueHelper.BrawnClassKey)
                    {
                        killerPlayer.UpdateStat(ScoreType.KillBrawn, 1);
                    }
                    else if (playerData?.Class == CatalogueHelper.SkillsClassKey)
                    {
                        killerPlayer.UpdateStat(ScoreType.KillSkills, 1);
                    }
                    else if (playerData?.Class == CatalogueHelper.BrainsClassKey)
                    {
                        killerPlayer.UpdateStat(ScoreType.KillBrains, 1);
                    }
                }
        
                UpdateStat(ScoreType.Deaths, 1);
        
                if (killerPlayer is not null && killerPlayer == killer)
                {
                    UpdateStat(ScoreType.KilledByHero, 1);
                    if (crit)
                    {
                        UpdateStat(ScoreType.KilledCriticalByHero, 1);
                    }
                    if (targetTeam != killerPlayer.Team)
                    { 
                        killerPlayer.UpdateStat(ScoreType.KillPlayerByHero, 1);
                        if (crit)
                        {
                            killerPlayer.UpdateStat(ScoreType.KillPlayerCriticalByHero, 1);
                        }
                    }
                }
                else if (killer?.UnitCard?.DeviceType is not DeviceType.None)
                {
                    UpdateStat(ScoreType.KilledByBlock, 1);
                    if (killerPlayer is not null && targetTeam != killerPlayer.Team)
                    {
                        killerPlayer.UpdateStat(ScoreType.KillPlayerByBlock, 1);
                    }
                }
                else
                {
                    UpdateStat(ScoreType.KilledByOther, 1);
                }
        
                var killerData = killerPlayer?.PlayerUnitData;
                if (killerData?.Class == CatalogueHelper.BrawnClassKey)
                {
                    UpdateStat(ScoreType.KilledByBrawn, 1);
                }
                else if (killerData?.Class == CatalogueHelper.SkillsClassKey)
                {
                    UpdateStat(ScoreType.KilledBySkills, 1);
                }
                else if (killerData?.Class == CatalogueHelper.BrainsClassKey)
                {
                    UpdateStat(ScoreType.KilledByBrains, 1);
                }

                foreach (var assister in assisters)
                {
                    assister.UpdateStat(ScoreType.Assists, 1);
                }
                RecentDamagers.Clear();
                break;
            
            case HealthType.World when killerPlayer is not null:
                switch (UnitCard.DeviceType)
                {
                    case DeviceType.None:
                    case DeviceType.World:
                        killerPlayer.UpdateStat(ScoreType.WorldDestroyed, 1);
                        break;
                    case DeviceType.Block:
                        killerPlayer.UpdateStat(ScoreType.BlocksDestroyed, 1);
                        break;
                    case DeviceType.Device:
                        killerPlayer.UpdateStat(ScoreType.DevicesDestroyed, 1);
                        break;
                    case DeviceType.Hero:
                        killerPlayer.UpdateStat(ScoreType.HeroBlocksDestroyed, 1);
                        break;
                }
                break;
            
            case HealthType.Objective when UnitCard?.IsObjective is true && killerPlayer is not null:
                killerPlayer.UpdateStat(UnitCard.IsBase ? ScoreType.KillBase : ScoreType.KillShield, 1);
                break;
        }
    }

    public void BuffStatsUpdate(bool isNegative, EffectSource source, Unit? buffingPlayer)
    {
        var playerByHero = isNegative ? ScoreType.DebuffPlayerByHero : ScoreType.BuffPlayerByHero;
        var byHero = isNegative ? ScoreType.DebuffedByHero : ScoreType.BuffedByHero;
        var playerByBlock = isNegative ? ScoreType.DebuffPlayerByBlock : ScoreType.BuffPlayerByBlock;
        var byBlock = isNegative ? ScoreType.DebuffedByBlock : ScoreType.BuffedByBlock;
        
        if (source is UnitSource { Unit.PlayerId: not null })
        {
            UpdateStat(byHero, 1);
            buffingPlayer?.UpdateStat(playerByHero, 1);
        }
        else
        {
            UpdateStat(byBlock, 1);
            buffingPlayer?.UpdateStat(playerByBlock, 1);
        }
    }

    public void MoveStatsUpdate(ZoneTransform transform, Vector3 oldPosition)
    {
        if (!transform.NoInterpolation)
        {
            UpdateStat(ScoreType.TravelDistance, Vector3.Distance(oldPosition, transform.Position));
        }

        if (transform.LocalVelocity == Vector3s.Zero)
        {
            TimeStoppedMoving.Start();
        }
        else
        {
            TimeStoppedMoving.Stop();
        }
    }

    public void StatsFromPickup(Unit pickup)
    {
        var pickupCard = pickup.UnitCard;
        if (pickupCard?.Labels?.Contains(UnitLabel.SupplyBlockbuster) is true)
        {
            UpdateStat(ScoreType.BlockbusterCollected, 1);
        }
        else if (pickupCard?.Labels?.Contains(UnitLabel.SupplyResource) is true)
        {
            UpdateStat(ScoreType.SupplyCollected, 1);
        }
    }

    public void UpdateStatsFromWin()
    {
        var playerData = PlayerUnitData;
        if (playerData?.Class == CatalogueHelper.BrawnClassKey)
        {
            UpdateStat(ScoreType.WinBrawn, 1);
        }
        else if (playerData?.Class == CatalogueHelper.SkillsClassKey)
        {
            UpdateStat(ScoreType.WinSkills, 1);
        }
        else if (playerData?.Class == CatalogueHelper.BrainsClassKey)
        {
            UpdateStat(ScoreType.WinBrains, 1);
        }
    }

    public void UpdateStatsFromTimers()
    {
        TimeRespawning.Stop();
        TimeSpotted.Stop();
        TimeControlled.Stop();
        TimeAtMaxHp.Stop();
        TimeAtLowHp.Stop();
        TimeStoppedMoving.Stop();

        UpdateStat(ScoreType.RespawnTime, (float)TimeRespawning.Elapsed.TotalSeconds);
        UpdateStat(ScoreType.SpottedTime, (float)TimeSpotted.Elapsed.TotalSeconds);
        UpdateStat(ScoreType.ControlledTime, (float)TimeControlled.Elapsed.TotalSeconds);
        UpdateStat(ScoreType.MaxHealthTime, (float)TimeAtMaxHp.Elapsed.TotalSeconds);
        UpdateStat(ScoreType.LowHealthTime, (float)TimeAtLowHp.Elapsed.TotalSeconds);
        UpdateStat(ScoreType.StandStillTime, (float)TimeStoppedMoving.Elapsed.TotalSeconds);
    }
}