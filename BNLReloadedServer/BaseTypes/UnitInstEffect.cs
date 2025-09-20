using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public partial class Unit
{
    public ImpactData CreateImpactDataExact(Vector3? insidePoint = null, Vector3? shotPos = null, Vector3s? normal = null,
        bool crit = false, Key? sourceKey = null) =>
        new()
        {
            InsidePoint = insidePoint ?? GetExactPosition(),
            Normal = normal ?? Vector3s.Zero,
            CasterUnitId = Id,
            CasterPlayerId = PlayerId,
            SourceKey = sourceKey,
            ShotPos = shotPos ?? GetExactPosition(),
            Crit = crit
        };

    public ImpactData CreateImpactData(Vector3? insidePoint = null, Vector3? shotPos = null, Vector3s? normal = null,
        bool crit = false, Key? sourceKey = null) =>
        new()
        {
            InsidePoint = insidePoint ?? Transform.Position,
            Normal = normal ?? Vector3s.Zero,
            CasterUnitId = Id,
            CasterPlayerId = PlayerId,
            SourceKey = sourceKey,
            ShotPos = shotPos ?? Transform.Position,
            Crit = crit
        };

    public void ApplyBuffEffects(float multiplier)
    {
        var unitUpdate = new UnitUpdate();
        var hasUpdate = false;
        foreach (var (buffKey, value) in Buffs)
        {
            switch (buffKey)
            {
                case BuffType.ResourceProduction:
                    AddResource(value * multiplier, ResourceType.General, unitUpdate);
                    hasUpdate = hasUpdate || unitUpdate.Resource != null;
                    break;
                case BuffType.HealthRegen:
                    break;
                case BuffType.ForcefieldRegen:
                    break;
                case BuffType.AmmoRegen:
                    AddAmmo(value * multiplier, unitUpdate);
                    hasUpdate = hasUpdate || unitUpdate.Ammo != null;
                    break;
                case BuffType.Bleeding:
                    break;
                case BuffType.Burning:
                    break;
                case BuffType.Poisoned:
                    break;
                case BuffType.Decay:
                    break;
                default:
                    continue;
            }
        }

        if (hasUpdate)
        {
            UpdateData(unitUpdate);
        }
    }

    private void AddAmmo(float amount, UnitUpdate update)
    {
        if (Gears.Count == 0 || CurrentGearIndex == -1) return;
        var currentGear = GetGearByIndex(CurrentGearIndex);
        if (currentGear?.Card.Ammo is not { } ammoData) return;
        var ammoUpdate = new Dictionary<Key, List<Ammo>> { { currentGear.Key, [] } };
        var sendUpdate = false;
        for (var i = 0; i < currentGear.Ammo.Count; i++)
        {
            var gearAmmo = currentGear.Ammo[i];
            ammoUpdate[currentGear.Key].Add(new Ammo { Index = gearAmmo.AmmoIndex, Mag = gearAmmo.Mag, Pool = gearAmmo.Pool });
            if (Math.Abs(gearAmmo.Pool - gearAmmo.PoolSize) < 0.001f) continue;
            if (ammoData[i].Pool is not { } pool) continue;
            ammoUpdate[currentGear.Key][i].Pool =
                Math.Min(float.FusedMultiplyAdd(this.AmmoGainAmount(amount), pool.BaseRegen, gearAmmo.Pool),
                    gearAmmo.PoolSize);
            sendUpdate = true;
        }

        if (sendUpdate)
        {
            update.Ammo = ammoUpdate;
        }
    }

    public void AddAmmo(float amount)
    {
        var update = new UnitUpdate();
        AddAmmo(amount, update);
        if (update.Ammo != null)
        { 
            UpdateData(update);
        }
    }

    private void AddAmmoPercent(float percentage, UnitUpdate update)
    {
        if (Gears.Count == 0 || CurrentGearIndex == -1) return;
        var currentGear = GetGearByIndex(CurrentGearIndex);
        if (currentGear?.Card.Ammo is not { } ammoData) return;
        var ammoUpdate = new Dictionary<Key, List<Ammo>> { { currentGear.Key, [] } };
        var sendUpdate = false;
        for (var i = 0; i < currentGear.Ammo.Count; i++)
        {
            var gearAmmo = currentGear.Ammo[i];
            ammoUpdate[currentGear.Key].Add(new Ammo { Index = gearAmmo.AmmoIndex, Mag = gearAmmo.Mag, Pool = gearAmmo.Pool });
            if (Math.Abs(gearAmmo.Pool - gearAmmo.PoolSize) < 0.001f) continue;
            if (ammoData[i].Pool is not { } pool) continue;
            ammoUpdate[currentGear.Key][i].Pool =
                Math.Min(float.FusedMultiplyAdd(percentage, pool.PoolSize, gearAmmo.Pool), gearAmmo.PoolSize);
            sendUpdate = true;
        }

        if (sendUpdate)
        {
            update.Ammo = ammoUpdate;
        }
    }

    public void AddAmmoPercent(float percentage)
    {
        var update = new UnitUpdate();
        AddAmmoPercent(percentage, update);
        if (update.Ammo != null)
        {
            UpdateData(update);
        }
    }

    private void AddResource(float amount, ResourceType source, UnitUpdate update)
    {
        var buffedAmount = this.ResourceGainAmount(amount, source);
        if (buffedAmount == 0 || (buffedAmount > 0 && Resource >= _updater.GetResourceCap()) || (buffedAmount < 0 && Resource <= 0)) return;
        
        update.Resource = float.Min(buffedAmount + Resource, _updater.GetResourceCap());
    }

    public void AddResource(float amount, ResourceType source)
    {
        var update = new UnitUpdate();
        AddResource(amount, source, update);
        if (update.Resource != null)
        {
            UpdateData(update);
        }
    }

    private void RemoveResources(float amount, UnitUpdate update)
    {
        if (amount > 0)
        {
            update.Resource = float.Max(0, Resource - amount);
        }
    }

    public void RemoveResources(float amount)
    {
        var update = new UnitUpdate();
        RemoveResources(amount, update);
        if (update.Resource != null)
        {
            UpdateData(update);
        }
    }
}