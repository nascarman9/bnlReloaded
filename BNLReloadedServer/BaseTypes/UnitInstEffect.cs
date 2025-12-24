using System.Numerics;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ServerTypes;

namespace BNLReloadedServer.BaseTypes;

public partial class Unit
{
    public ImpactData CreateImpactDataExact(Vector3? insidePoint = null, Vector3? shotPos = null, Vector3s? normal = null,
        bool crit = false, Key? sourceKey = null, uint? casterId = null, uint? casterPlayerId = null) =>
        new()
        {
            InsidePoint = insidePoint ?? GetExactPosition(),
            Normal = normal ?? Vector3s.Zero,
            CasterUnitId = casterId ?? Id,
            CasterPlayerId = casterPlayerId ?? OwnerPlayerId,
            SourceKey = sourceKey ?? (UnitCard?.KillscoreIcon?.Length is > 0 ? Key : CatalogueHelper.DefaultSource),
            ShotPos = shotPos ?? GetExactPosition(),
            Crit = crit
        };

    public ImpactData CreateImpactData(Vector3? insidePoint = null, Vector3? shotPos = null, Vector3s? normal = null,
        bool crit = false, Key? sourceKey = null, uint? casterId = null, uint? casterPlayerId = null) =>
        new()
        {
            InsidePoint = insidePoint ?? GetMidpoint(),
            Normal = normal ?? Vector3s.Zero,
            CasterUnitId = casterId ?? Id,
            CasterPlayerId = casterPlayerId ?? OwnerPlayerId,
            SourceKey = sourceKey ?? (UnitCard?.KillscoreIcon?.Length is > 0 ? Key : CatalogueHelper.DefaultSource),
            ShotPos = shotPos ?? GetMidpoint(),
            Crit = crit
        };

    public ImpactData CreateBlankImpactData() =>
        new()
        {
            InsidePoint = GetMidpoint(),
            Normal = Vector3s.Zero,
            ShotPos = GetMidpoint(),
            Crit = false
        };

    public void ApplyBuffEffects(float multiplier)
    {
        var unitUpdate = new UnitUpdate();
        var hasUpdate = false;
        var healthType = UnitCard?.Health?.Health?.HealthType;
        foreach (var (buffKey, value) in _buffs)
        {
            // Buff * multiplier
            switch (buffKey, healthType)
            {
                case (BuffType.ResourceProduction, _):
                    AddResource(value * multiplier, ResourceType.General, unitUpdate);
                    hasUpdate = hasUpdate || unitUpdate.Resource != null;
                    break;
                
                case (BuffType.HealthRegen, _):
                    var healAmount = AddHealth(value * multiplier, unitUpdate);

                    if (healAmount > 0)
                    {
                        var healSource = ActiveEffects.Find(e =>
                        {
                            var eCard = e.Card;
                            return eCard.Effect is ConstEffectBuff { Buffs: not null } eff &&
                                   eff.Buffs.ContainsKey(buffKey);
                        });
                        
                        if (healSource is not null)
                        {
                            var sourceList = _effectSources.GetValueOrDefault(healSource.Key);
                            if (sourceList is { Count: > 0 })
                            {
                                var hSource = sourceList.First();
                                Healed(healAmount, hSource,
                                    hSource.Impact?.CasterPlayerId is not null
                                        ? _updater.GetPlayerFromPlayerId(hSource.Impact.CasterPlayerId.Value)
                                        : null);
                            }
                        }
                    }
                    hasUpdate = hasUpdate || unitUpdate.Health != null;
                    break;
                
                case (BuffType.ForcefieldRegen, _):
                    AddForcefield(value * multiplier, unitUpdate);
                    hasUpdate = hasUpdate || unitUpdate.Forcefield != null;
                    break;
                
                case (BuffType.AmmoRegen, _):
                    AddAmmo(value * multiplier, unitUpdate);
                    hasUpdate = hasUpdate || unitUpdate.Ammo != null;
                    break;
                
                case (BuffType.Bleeding, HealthType.Player or HealthType.World):
                case (BuffType.Burning, HealthType.Player):
                case (BuffType.Poisoned, HealthType.Player):
                case (BuffType.Decay, HealthType.World):
                    var effectSource = ActiveEffects.Find(e =>
                    {
                        var eCard = e.Card;
                        return eCard.Effect is ConstEffectBuff { Buffs: not null } eff &&
                               eff.Buffs.ContainsKey(buffKey);
                    });
                    
                    EffectSource? source = null;
                    if (effectSource is not null)
                    {
                        source = _effectSources.GetValueOrDefault(effectSource.Key)?.FirstOrDefault();
                    }
                    
                    TakeDamage(value * multiplier, source, unitUpdate);
                    hasUpdate = hasUpdate || unitUpdate.Health != null || unitUpdate.Forcefield != null || unitUpdate.Ammo != null;
                    break;
            }
        }

        if (hasUpdate)
        {
            UpdateData(unitUpdate);
        }
    }

    private void AddAmmo(float amount, UnitUpdate update)
    {
        if (Gears.Count == 0 || amount <= 0) return;
        var ammoUpdate = new Dictionary<Key, List<Ammo>>();
        var sendUpdate = false;
        foreach (var gear in Gears)
        {
            if (gear.Card.Ammo is not { } ammoData) continue;
            ammoUpdate.Add(gear.Key, []);
            for (var i = 0; i < gear.Ammo.Count; i++)
            {
                var gearAmmo = gear.Ammo[i];
                ammoUpdate[gear.Key].Add(new Ammo
                    { Index = gearAmmo.AmmoIndex, Mag = gearAmmo.Mag, Pool = gearAmmo.Pool });
                if (gearAmmo.Pool >= gearAmmo.PoolSize) continue;
                if (ammoData[i].Pool is not { } pool) continue;
                ammoUpdate[gear.Key][i].Pool =
                    Math.Min(float.FusedMultiplyAdd(this.AmmoGainAmount(amount), pool.BaseRegen, gearAmmo.Pool),
                        gearAmmo.PoolSize);
                sendUpdate = true;
            }
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
        if (Gears.Count == 0 || percentage <= 0) return;
        var ammoUpdate = new Dictionary<Key, List<Ammo>>();
        var sendUpdate = false;
        foreach (var gear in Gears)
        {
            if (gear.Card.Ammo is not { } ammoData) continue;
            ammoUpdate.Add(gear.Key, []);
            for (var i = 0; i < gear.Ammo.Count; i++)
            {
                var gearAmmo = gear.Ammo[i];
                ammoUpdate[gear.Key].Add(new Ammo
                    { Index = gearAmmo.AmmoIndex, Mag = gearAmmo.Mag, Pool = gearAmmo.Pool });
                if (gearAmmo.Pool >= gearAmmo.PoolSize) continue;
                if (ammoData[i].Pool is not { } pool) continue;
                ammoUpdate[gear.Key][i].Pool =
                    Math.Min(float.FusedMultiplyAdd(percentage, pool.PoolSize, gearAmmo.Pool), gearAmmo.PoolSize);
                sendUpdate = true;
            }
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

    private void TakeAmmo(float amount, bool fromMag, UnitUpdate update)
    {
        if (Gears.Count == 0 || _currentGearIndex == -1) return;
        var currentGear = GetGearByIndex(_currentGearIndex);
        if (currentGear?.Card.Ammo is null ) return;
        var ammoUpdate = new Dictionary<Key, List<Ammo>> { { currentGear.Key, [] } };
        var sendUpdate = false;
        for (var i = 0; i < currentGear.Ammo.Count; i++)
        {
            var gearAmmo = currentGear.Ammo[i];
            var fromPool = !fromMag || !gearAmmo.IsMag;
            ammoUpdate[currentGear.Key].Add(new Ammo { Index = gearAmmo.AmmoIndex, Mag = gearAmmo.Mag, Pool = gearAmmo.Pool });
            switch (fromPool)
            {
                case true when gearAmmo.Pool == 0:
                case false when gearAmmo.Mag == 0:
                    continue;
                case true:
                {
                    ammoUpdate[currentGear.Key][i].Pool = Math.Max(gearAmmo.Pool - amount, 0);
                    break;
                }
                default:
                    ammoUpdate[currentGear.Key][i].Mag = Math.Max(gearAmmo.Mag - amount, 0);
                    break;
            }

            sendUpdate = true;
        }

        if (sendUpdate)
        {
            update.Ammo = ammoUpdate;
        }
    }
    
    public void TakeAmmo(float amount, bool fromMag)
    {
        var update = new UnitUpdate();
        TakeAmmo(amount, fromMag, update);
        if (update.Ammo != null)
        { 
            UpdateData(update);
        }
    }

    private void AddResource(float amount, ResourceType source, UnitUpdate update)
    {
        var currResource = update.Resource ?? Resource;
        var buffedAmount = this.ResourceGainAmount(amount, source);
        if (source == ResourceType.Mining)
        {
            var digHpBuff = buffedAmount * GetBuff(BuffType.MiningHealthRefill);
            if (digHpBuff > 0.0f)
            {
                AddHealth(digHpBuff, update);
            }

            var digAmmoBuff = buffedAmount * GetBuff(BuffType.MiningAmmoRefill);
            if (digAmmoBuff > 0.0f)
            {
                AddAmmo(digAmmoBuff, update);
            }
        }
        if (buffedAmount == 0 || (buffedAmount > 0 && currResource >= _updater.GetResourceCap()) || (buffedAmount < 0 && currResource <= 0)) return;
        
        var resourceAmount = float.Min(buffedAmount + currResource, _updater.GetResourceCap());
        update.Resource = resourceAmount;
        
        EarnedResource(resourceAmount - currResource, source is ResourceType.Mining);
    }

    public void AddResource(float amount, ResourceType source)
    {
        var update = new UnitUpdate();
        AddResource(amount, source, update);
        if (update.Resource != null || update.Ammo != null || update.Health != null)
        {
            UpdateData(update);
        }
    }

    private void RemoveResources(float amount, UnitUpdate update)
    {
        var currResource = update.Resource ?? Resource;
        if (amount > 0)
        {
            update.Resource = float.Max(0, currResource - amount);
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

    private float AddHealth(float amount, UnitUpdate update)
    {
        var currHealth = update.Health ?? _health;
        if (amount <= 0 || UnitCard?.Health?.Health is not {} health || currHealth >= this.UnitMaxHealth(health.MaxHealth)) return 0;

        var healAmount = MathF.Min(currHealth + this.HealthGainAmount(amount), this.UnitMaxHealth(health.MaxHealth));
        update.Health = healAmount;
        return healAmount;
    }

    public float AddHealth(float amount)
    {
        var update = new UnitUpdate();
        var healAmount = AddHealth(amount, update);
        if (update.Health != null)
        {
            UpdateData(update);
        }

        return healAmount;
    }

    private void AddForcefield(float amount, UnitUpdate update)
    {
        var currForcefield = update.Forcefield ?? _forcefield;
        if (amount <= 0 || UnitCard?.Health?.Forcefield is not {} forcefield || currForcefield >= this.UnitMaxForcefield(forcefield.MaxAmount)) return;

        update.Forcefield = MathF.Min(currForcefield + amount, this.UnitMaxForcefield(forcefield.MaxAmount));
    }

    public void AddForcefield(float amount)
    {
        var update = new UnitUpdate();
        AddForcefield(amount, update);
        if (update.Forcefield != null)
        {
            UpdateData(update);
        }
    }

    private void AddSupplies(float amount, UnitUpdate update)
    {
        if (amount <= 0) return;
        var hpBuff = amount * GetBuff(BuffType.SupplyHealth);
        if (hpBuff > 0.0f)
        {
            AddHealth(hpBuff, update);
        }

        var forceBuff = amount * GetBuff(BuffType.SupplyForcefield);
        if (forceBuff > 0.0f)
        {
            AddForcefield(forceBuff, update);
        }
        
        var ammoBuff = amount * GetBuff(BuffType.SupplyAmmo);
        if (ammoBuff > 0.0f)
        {
            AddAmmo(ammoBuff, update);
        }
    }

    public void AddSupplies(float amount)
    {
        var update = new UnitUpdate();
        AddSupplies(amount, update);
        if (update.Health != null || update.Forcefield != null || update.Ammo != null)
        {
            UpdateData(update);
        }
    }

    private void TakeDamage(float amount, EffectSource? source, UnitUpdate update)
    {
        if (!IsHealth || IsDead || GetBuff(BuffType.Invulnerability) > 0) return;

        var currHealth = update.Health ?? _health;
        var newHealth = Math.Max(0.0f, currHealth - amount);
        update.Health = newHealth;
        var impact = source?.Impact ?? new ImpactData
        {
            InsidePoint = GetMidpoint(),
            Normal = Vector3s.Zero,
            CasterUnitId = null,
            CasterPlayerId = null,
            Impact = null,
            SourceKey = null,
            HitUnits = [Id],
            ShotPos = GetMidpoint(),
            Crit = false
        };
        
        _updater.OnUnitDamaged(this, amount, impact);
        
        if ((update.Health ?? _health) <= 0.0f)
        {
            Killed(impact, false, update);
        }
    }
    
    private void TakeDamage(DamageData damage, ImpactData impact, bool splash, Unit? attacker, TeamType? attackingTeam, UnitUpdate update)
    {
        if (!IsHealth || IsDead) return;
        if ((!damage.IgnoreInvincibility && GetBuff(BuffType.Invulnerability) > 0) ||
            ((((UnitCard?.Health?.Health?.MeleeOnly ?? false) && !damage.Melee) ||
            ((UnitCard?.Health?.Health?.MiningOnly ?? false) && !damage.Mining) ||
            UnitCard?.Data?.Type is UnitType.DamageCapture) && !damage.IgnoreDefences) || HasSpawnProtection)
        {
            //_updater.OnUnitDamaged(this, 0, impact);
            return;
        }
        
        var attTeam = attackingTeam ?? attacker?.Team;

        float dmg;
        switch (UnitCard?.Health?.Health?.HealthType)
        {
            case HealthType.Player:
                if (attacker?.OwnerPlayerId == PlayerId && attTeam == Team)
                {
                    if (damage.IgnoreDefences)
                    {
                        dmg = damage.SelfDamage;
                    }
                    else if (splash)
                    {
                        dmg = this.SplashDamageTaken(damage.SelfDamage);
                    }
                    else
                    {
                        dmg = this.DamageTaken(damage.SelfDamage);
                    }
                }
                else if (attTeam == Team)
                {
                    if (damage.IgnoreDefences)
                    {
                        dmg = damage.FriendDamage;
                    }
                    else if (splash)
                    {
                        dmg = this.SplashDamageTaken(damage.FriendDamage);
                    }
                    else
                    {
                        dmg = this.DamageTaken(damage.FriendDamage);
                    }
                }
                else
                {
                    if (damage.IgnoreDefences)
                    {
                        dmg = damage.EnemyDamage;
                    }
                    else if (splash)
                    {
                        dmg = this.SplashDamageTaken(damage.EnemyDamage);
                    }
                    else
                    {
                        dmg = this.DamageTaken(damage.EnemyDamage);
                    }
                }
                break;
            case HealthType.World:
                if (attacker?.Id != Id && attTeam == Team)
                {
                    if (damage.IgnoreDefences)
                    {
                        dmg = damage.TeamDeviceDamage;
                    }
                    else if (splash)
                    {
                        dmg = this.SplashDamageTaken(damage.TeamDeviceDamage);
                    }
                    else
                    {
                        dmg = this.DamageTaken(damage.TeamDeviceDamage);
                    }
                }
                else
                {
                    if (damage.IgnoreDefences)
                    {
                        dmg = damage.EnemyDeviceDamage;
                    }
                    else if (splash)
                    {
                        dmg = this.SplashDamageTaken(damage.EnemyDeviceDamage);
                    }
                    else
                    {
                        dmg = this.DamageTaken(damage.EnemyDeviceDamage);
                    }
                }
                break;
            case HealthType.Objective:
                if (attTeam == Team)
                {
                    if (damage.IgnoreDefences)
                    {
                        dmg = damage.TeamObjectiveDamage;
                    }
                    else if (splash)
                    {
                        dmg = this.SplashDamageTaken(damage.TeamObjectiveDamage);
                    }
                    else
                    {
                        dmg = this.DamageTaken(damage.TeamObjectiveDamage);
                    }
                }
                else
                {
                    if (damage.IgnoreDefences)
                    {
                        dmg = damage.EnemyObjectiveDamage;
                    }
                    else if (splash)
                    {
                        dmg = this.SplashDamageTaken(damage.EnemyObjectiveDamage);
                    }
                    else
                    {
                        dmg = this.DamageTaken(damage.EnemyObjectiveDamage);
                    }
                }
                break;
            default:
                return;
        }

        dmg = Math.Max(dmg - UnitCard.Health.Health.Toughness, 0.0f);
        if (dmg <= 0)
        {
            return;
        }

        (float Health, float Forcefield, float Shield) status = (update.Health ?? _health,
            update.Forcefield ?? _forcefield, update.Shield ?? _shield);
        
        switch (status, damage)
        {
            case ({ Forcefield: > 0 }, _):
                var currForcefield = status.Forcefield;
                var newForcefield = Math.Max(0.0f, currForcefield - dmg);
                update.Forcefield = newForcefield;
                var forceDmg = currForcefield - newForcefield;
                _updater.OnUnitDamaged(this, forceDmg, impact);
                if (forceDmg > 0 && UnitCard?.Health?.Forcefield is { } forcefield)
                {
                    _rechargeForcefieldTime = DateTimeOffset.Now.AddSeconds(update.Forcefield > 0
                        ? forcefield.HitRechargeDelay
                        : forcefield.EmptyRechargeDelay);
                }
                break;
            
            case ({ Shield: > 0 }, { IgnoreDefences: false }):
                var currShield = status.Shield;
                var newShield = Math.Max(0.0f, currShield - dmg);
                update.Shield = newShield;
                if (newShield <= 0)
                {
                    var curr2Health = status.Health;
                    var new2Health = Math.Max(0.0f, curr2Health - (dmg - (currShield - newShield)));
                    update.Health = new2Health;
                }
                _updater.OnUnitDamaged(this, dmg, impact);
                
                if (_rechargeForcefieldTime is not null && UnitCard?.Health?.Forcefield is { } force)
                {
                    _rechargeForcefieldTime = DateTimeOffset.Now.AddSeconds(force.EmptyRechargeDelay);
                }
                break;
            
            case ({ Health: > 0 }, _):
                var currHealth = status.Health;
                var newHealth = Math.Max(0.0f, currHealth - dmg);
                update.Health = newHealth;
                _updater.OnUnitDamaged(this, dmg, impact); 
                
                if (_rechargeForcefieldTime is not null && UnitCard?.Health?.Forcefield is { } frc)
                {
                    _rechargeForcefieldTime = DateTimeOffset.Now.AddSeconds(frc.EmptyRechargeDelay);
                }
                break;
            
            default:
                return;
        }

        if (impact.Impact != CatalogueHelper.AntimatterShieldImpact)
        {
            var selfImpact = CreateImpactData();
            foreach (var effect in ActiveEffects.GetEffectsOfType<ConstEffectOnDamageTaken>().Select(d => d.Effect).OfType<InstEffect>())
            {
                _updater.OnApplyInstEffect(GetSelfSource(selfImpact), [this], effect, selfImpact);
            }
        }
        else
        {
            impact.SourceKey = CatalogueHelper.AntimatterSource;
        }
        
        // Check if unit has died
        if (UnitCard?.Data is UnitDataBomb { TriggerOnDamage: true })
        {
            _bombTimeoutEnd = DateTimeOffset.Now;
            Killed(CreateBlankImpactData(), false, update);
        }
        else if ((update.Health ?? _health) <= 0.0f)
        {
            Killed(impact, damage.Mining, update);
        }
    }

    public void TakeDamage(float damage, EffectSource? source)
    {
        var update = new UnitUpdate();
        TakeDamage(damage, source, update);
        if (update.Health != null || update.Forcefield != null || update.Shield != null)
        {
            UpdateData(update);
        }
    }

    public void TakeDamage(DamageData damage, ImpactData impact, bool splash, Unit? attacker, TeamType? attackingTeam)
    {
        var update = new UnitUpdate();
        TakeDamage(damage, impact, splash, attacker, attackingTeam, update);
        if (update.Health != null || update.Forcefield != null || update.Shield != null)
        {
            UpdateData(update);
        }
    }
    
    private void Killed(ImpactData impact, bool mining, UnitUpdate update)
    {
        if (IsDead) return;
        var selfImpact = CreateImpactData();
        foreach (var effect in ActiveEffects.GetEffectsOfType<ConstEffectOnDeath>().Select(dth => dth.Effect).OfType<InstEffect>())
        {
            _updater.OnApplyInstEffect(GetSelfSource(selfImpact), [this], effect, selfImpact);
        }

        if (DamageCaptureEffect?.NearbyUnits is { Length: > 0 } && UnitCard?.Data is UnitDataDamageCapture
            { ZoneEffects.Count: > 0 } dataDamageCapture)
        {
            DamageCaptureEffect?.NearbyUnits.ToList().ForEach(u =>
                u.RemoveEffects(
                    dataDamageCapture.ZoneEffects.Select(e => new ConstEffectInfo(e)),
                    Team, GetSelfSource(selfImpact)));
        }
        
        OnDestroyed?.Invoke();
        _updater.OnUnitKilled(this, impact, mining);
        IsDead = true;
        LastMoveTime = null;
        
        if (IsActive && PlayerId != null)
        {
            TimeRespawning.Start();
        }
        
        if (IsHealth)
        {
           update.Health = 0.0f; 
        }

        _returnOnRevive =
            ActiveEffects
                .Where(e => _effectSources.GetValueOrDefault(e.Key)?.Any(s => s is PersistOnDeathSource) ?? false)
                .ToDictionary(k => k, v => (PersistOnDeathSource)_effectSources[v.Key].First(f => f is PersistOnDeathSource));
        update.Effects = new Dictionary<Key, ulong?>();
        _effectSources.Clear();
    }

    public void Killed(ImpactData impact, bool mining = false)
    {
        var update = new UnitUpdate();
        Killed(impact, mining, update);
        UpdateData(update);
    }

    private void ChargeTesla(int chargeAmount, UnitUpdate update)
    {
        if (chargeAmount == 0 || TeslaUnitData is null) return;

        var oldCharges = _charges;
        _charges = int.Clamp(_charges + chargeAmount, 0, TeslaUnitData.MaxCharges);
        if (_charges == oldCharges) return;
        
        if (_charges == 0)
        {
            update.TeslaCharge = TeslaChargeType.NoCharge;
        }
        else if (_charges < TeslaUnitData.MaxCharges)
        {
            update.TeslaCharge = TeslaChargeType.SelfCharge;
        }
        else
        {
            update.TeslaCharge = TeslaChargeType.FullSelfCharge;
        }
    }

    public void ChargeTesla(int chargeAmount)
    {
        var update = new UnitUpdate();
        ChargeTesla(chargeAmount, update);
        if (update.TeslaCharge != null)
        {
            UpdateData(update);
        }
    }

    public void PickupTaken(Unit recipient)
    {
        CanPickUp = false;
        Killed(recipient.CreateImpactData(insidePoint: GetMidpoint(), shotPos: GetMidpoint()));
    }

    private void AbilityUsed(UnitUpdate update)
    {
        if (AbilityCard is not { Charges: not null } aCard) return;
        var currCharges = update.AbilityCharges ?? AbilityCharges;
        if (currCharges == 0) return;

        currCharges -= 1;
        UpdateStat(ScoreType.AbilityUsed, 1);
        
        update.AbilityCharges = currCharges;
        TimeTillNextAbilityCharge ??= DateTimeOffset.Now.AddSeconds(this.AbilityCooldownTime(aCard.Charges.ChargeCooldown));
        if (update.AbilityCharges < aCard.Charges.MaxCharges)
        {
            update.AbilityChargeCooldownEnd = (ulong)TimeTillNextAbilityCharge.Value.ToUnixTimeMilliseconds();
        }
    }

    public void AbilityUsed()
    {
        var update = new UnitUpdate();
        AbilityUsed(update);
        if (update.AbilityCharges != null)
        {
            UpdateData(update);
        }
    }

    private void AbilityChargeGained(UnitUpdate update)
    {
        if (AbilityCard is not { Charges: not null } aCard) return;
        update.AbilityCharges = Math.Min((update.AbilityCharges ?? AbilityCharges) + 1, aCard.Charges.MaxCharges);

        TimeTillNextAbilityCharge = update.AbilityCharges < aCard.Charges.MaxCharges
            ? DateTimeOffset.Now.AddSeconds(this.AbilityCooldownTime(aCard.Charges.ChargeCooldown))
            : null;

        if (update.AbilityCharges < aCard.Charges.MaxCharges && TimeTillNextAbilityCharge.HasValue)
        {
            update.AbilityChargeCooldownEnd = (ulong)TimeTillNextAbilityCharge.Value.ToUnixTimeMilliseconds();
        }
    }

    public void AbilityChargeGained()
    {
        var update = new UnitUpdate();
        AbilityChargeGained(update);
        if (update.AbilityCharges != null)
        {
            UpdateData(update);
        }
    }

    public void SetTurretTarget(uint targetId)
    {
        var update = new UnitUpdate
        {
            TurretTargetId = targetId
        };
        
        UpdateData(update);
    }

    private void OnDisabled()
    {
        var enabledEffects = UnitCard?.EnabledEffects;

        if (enabledEffects is { Count: > 0 })
        {
            RemoveEffects(enabledEffects.Select(e => new ConstEffectInfo(e)), Team, null);
        }

        if (PortalLinked.LinkedPortalUnitId is not null)
        {
            _updater.LinkPortal(this, true);
        }
        
        _disabledTime = DateTimeOffset.Now;
        _wasDisabled = true;
    }

    private void OnReEnabled()
    {
        var enabledEffects = UnitCard?.EnabledEffects;

        if (enabledEffects is { Count: > 0 })
        {
            AddEffects(enabledEffects.Select(e => new ConstEffectInfo(e)), Team, null);
        }

        if (UnitCard?.Data is UnitDataPortal)
        {
            _updater.LinkPortal(this);
        }

        if (_bombTimeoutEnd is not null && _disabledTime is not null)
        {
            UpdateData(new UnitUpdate
            {
                BombTimeoutEnd = (ulong)(_bombTimeoutEnd.Value + (_disabledTime.Value - DateTimeOffset.Now)).ToUnixTimeMilliseconds()
            });
            _disabledTime = null;
        }
        
        _wasDisabled = false;
    }

    private void OnConfused(Unit confuser)
    {
        OwnerPlayerId = confuser.OwnerPlayerId;
        var impact = CreateImpactData();
        var source = GetSelfSource(impact);
        
        foreach (var effect in UnitsInAuraSinceLastUpdate.Keys)
        {
            var unitList = UnitsInAuraSinceLastUpdate[effect];
            if (unitList.Length > 0)
            {
                if (effect.LeaveEffect != null)
                {
                    _updater.OnApplyInstEffect(source, unitList, effect.LeaveEffect, impact);
                }

                if (effect.ConstantEffects != null)
                {
                    unitList.ToList().ForEach(u =>
                        u.RemoveEffects(effect.ConstantEffects.Select(e => new ConstEffectInfo(e)),
                            Team, source));
                }
            }
            UnitsInAuraSinceLastUpdate[effect] = [];
        }
        
        WinningTeam = TeamType.Neutral;
        UpdateData(new UnitUpdate
        {
            Team = confuser.Team
        });
        _wasConfused = true;
        _everConfused = true;
    }

    private void OnUnconfused()
    {
        OwnerPlayerId = PermaOwnerPlayerId;
        var impact = CreateImpactData();
        var source = GetSelfSource(impact);
        
        foreach (var effect in UnitsInAuraSinceLastUpdate.Keys)
        {
            var unitList = UnitsInAuraSinceLastUpdate[effect];
            if (unitList.Length > 0)
            {
                if (effect.LeaveEffect != null)
                {
                    _updater.OnApplyInstEffect(source, unitList, effect.LeaveEffect, impact);
                }

                if (effect.ConstantEffects != null)
                {
                    unitList.ToList().ForEach(u =>
                        u.RemoveEffects(effect.ConstantEffects.Select(e => new ConstEffectInfo(e)),
                            Team, source));
                }
            }
            UnitsInAuraSinceLastUpdate[effect] = [];
        }
        

        WinningTeam = TeamType.Neutral;
        UpdateData(new UnitUpdate
        {
            Team = PermaTeam
        });
        _wasConfused = false;
    }

    private bool DoPull(Unit puller, ConstEffectPull pull)
    {
        if (pull.Force < _minPullForce && DateTimeOffset.Now - _lastPullTime < TimeSpan.FromSeconds(3)) return false;
        if (Math.Abs(pull.Force - _minPullForce) < 0.01f && _activePuller is not null &&
            puller.Id != _activePuller) return false;

        _minPullForce = pull.Force;
        _lastPullTime = DateTimeOffset.Now;
        _activePuller = puller.Id;
        
        return true;
    }

    public void OnFall(float height, bool force, float min, float max, bool doFallDamage)
    {
        var fallPoint = GetFallPosition();
        
        var selfImpact = CreateImpactData(insidePoint: fallPoint, shotPos: fallPoint);
        foreach (var effect in ActiveEffects.GetEffectsOfType<ConstEffectOnFall>().Where(dth => dth.Effect != null))
        {
            if ((effect.ForceOnly && !force) || effect.MinHeight > height || effect.Effect is null) continue;
            _updater.OnApplyInstEffect(GetSelfSource(selfImpact), [this], effect.Effect, selfImpact);
        }

        var unitCard = UnitCard;

        if (unitCard?.Data is UnitDataBomb { FallTimerResetHeightLimit: not null } bombData && bombData.FallTimerResetHeightLimit <= height)
        {
            UpdateData(new UnitUpdate
            {
                BombTimeoutEnd = (ulong)DateTimeOffset.Now.AddSeconds(bombData.Timeout).ToUnixTimeMilliseconds()
            });
        }
        
        if (!doFallDamage || unitCard?.FallHitModifier is 0 || height < min || IsDead || unitCard?.Health?.Health is null) return;
        
        var fallNormal = Vector3.UnitY;
        var fallImpact = CreateImpactData(insidePoint: Transform.Position + fallNormal, normal: (Vector3s)fallNormal,
            shotPos: Transform.Position + fallNormal, sourceKey: CatalogueHelper.FallSource);
        fallImpact.HitUnits = [Id];
        fallImpact.Impact = CatalogueHelper.FallImpact;
        fallImpact.CasterPlayerId = null;
        fallImpact.CasterUnitId = null;

        var maxHp = this.UnitMaxHealth(unitCard.Health.Health.MaxHealth);
        var fallCoeff = max > 0 && max > min
            ? float.Clamp(
                (height - min) / (max - min) * (unitCard.FallHitModifier *
                                                Math.Max(0.0f, 1f - GetBuff(BuffType.FallDamageReduction))), 0, 1)
            : 1;

        var dmgAmount = float.Lerp(0, maxHp, fallCoeff);
        var damage = new DamageData(dmgAmount, 0, 0, dmgAmount, 0, 0, dmgAmount, 0, false, false, false, true);
        TakeDamage(damage, fallImpact, false, this, null);
    }

    public bool Respawn(Vector3 spawnPosition, Quaternion spawnRotation, Vector3s? vectorSpawnRotation = null)
    {
        if (ZoneService is null) return false;
        RespawnTime = null;
        WinningTeam = TeamType.Neutral;
        IsDead = false;
        Id = _updater.OnChangeId(this);
        LastMoveTime = DateTimeOffset.Now;
        WasAfkWarned = false;
        if (vectorSpawnRotation is not null)
        {
            Transform = new ZoneTransform
            {
                Position = spawnPosition,
                Rotation = vectorSpawnRotation.Value
            };
        }
        else
        {
            Transform = ZoneTransformHelper.ToZoneTransform(spawnPosition, spawnRotation);
        }

        var initData = GetInitData();
        
        var startingEffects = InitialEffects.ToDictionary();

        foreach (var effect in _updater.GetTeamEffects(Team).Where(e => DoesEffectApply(e, Team)))
        {
            if (startingEffects.TryGetValue(effect.Key, out var value))
            {
                if (value.HasValue && (!effect.HasDuration || effect.TimestampEnd > value))
                {
                    startingEffects[effect.Key] = effect.TimestampEnd;
                }
            }
            else
            {
                startingEffects.Add(effect.Key, effect.TimestampEnd);
            }
        }

        if (_returnOnRevive is not null)
        {
            foreach (var effect in _returnOnRevive.Keys)
            {
                if (startingEffects.TryGetValue(effect.Key, out var value))
                {
                    if (value.HasValue && (!effect.HasDuration || effect.TimestampEnd > value))
                    {
                        startingEffects[effect.Key] = effect.TimestampEnd;
                    }
                }
                else
                {
                    startingEffects.Add(effect.Key, effect.TimestampEnd);
                }
            }
            
            foreach (var (effect, source) in _returnOnRevive)
            {
                if (_effectSources.TryGetValue(effect.Key, out var sourceEffects))
                {
                    sourceEffects.Add(source);
                }
                else
                {
                    _effectSources.Add(effect.Key, [source]);
                }
            }
        }

        _returnOnRevive = null;

        ActiveEffects = ConstEffectInfo.Convert(startingEffects);
        
        foreach (var ammo in Gears.SelectMany(gear => gear.Ammo))
        {
            ammo.Mag = ammo.MagSize;
            ammo.Pool = ammo.PoolSize;
        }
        
        Dictionary<Key, List<Ammo>> updateAmmo = [];
        foreach (var gear in Gears)
        {
            var ammo = gear.Ammo
                .Select(ammo => new Ammo { Index = ammo.AmmoIndex, Mag = ammo.Mag, Pool = ammo.Pool }).ToList();
            updateAmmo.Add(gear.Key, ammo);
        }

        var uCard = UnitCard;
        
        TimeRespawning.Stop();
        
        _updater.OnRespawn(this, initData, ZoneService);
        IsDropped = false;
        UpdateData(new UnitUpdate
        {
            Team = Team,
            Health = uCard?.Health?.Health != null
                ? this.UnitMaxHealth(uCard.Health.Health.MaxHealth)
                : null,
            Forcefield = uCard?.Health?.Forcefield != null
                ? this.UnitMaxForcefield(uCard.Health.Forcefield.MaxAmount)
                : null,
            Shield = uCard?.Health?.Health?.Shield,
            Ammo = updateAmmo,
            MovementActive = true,
            CurrentGear = CurrentGear?.Key,
            Ability = AbilityCard?.Key,
            AbilityCharges = AbilityCharges,
            AbilityChargeCooldownEnd = (ulong)(TimeTillNextAbilityCharge?.ToUnixTimeMilliseconds() ?? 0),
            Resource = Resource,
            Effects = ActiveEffects.ToInfoDictionary(),
            Devices = Devices
        }, unbuffered: true);
        
        return true;
    }

    public void RecallEnded()
    {
        RecallTime = null;
        IsRecall = false;
    }
}