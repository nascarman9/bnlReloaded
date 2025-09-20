using System.Collections.Immutable;
using System.Numerics;
using System.Timers;
using BNLReloadedServer.Database;
using BNLReloadedServer.Octree_Extensions;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Service;
using ObservableCollections;
using Timer = System.Timers.Timer;

namespace BNLReloadedServer.BaseTypes;

internal record IntervalUpdater
{
    private readonly Timer _timer;

    public IntervalUpdater(float Interval,
        List<InstEffect> IntervalEffects,
        Func<bool> ConstEffectCheck,
        Func<Unit[]> GetAffectedUnits,
        Action<IEnumerable<Unit>, InstEffect> OnApplyInstEffect)
    {
        this.Interval = Interval;
        this.IntervalEffects = IntervalEffects;
        this.ConstEffectCheck = ConstEffectCheck;
        this.GetAffectedUnits = GetAffectedUnits;
        this.OnApplyInstEffect = OnApplyInstEffect;
        
        _timer = new Timer(TimeSpan.FromSeconds(Interval));
        _timer.Elapsed += OnIntervalElapsed;
        _timer.AutoReset = true;
        _timer.Start();
    }
    
    public float Interval { get; }
    public List<InstEffect> IntervalEffects { get; }
    public Func<bool> ConstEffectCheck { get; }
    public Func<Unit[]> GetAffectedUnits { get; }
    public Action<IEnumerable<Unit>, InstEffect> OnApplyInstEffect { get; }

    private void OnIntervalElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!ConstEffectCheck())
        {
            _timer.Stop();
            _timer.Dispose();
            return;
        }
        
        var affectedUnits = GetAffectedUnits();
        if (affectedUnits.Length == 0) return;
        foreach (var effect in IntervalEffects)
        {
            OnApplyInstEffect(affectedUnits, effect);
        }
    }
}

public partial class Unit
{
    public readonly uint Id;
    public TeamType Team;
    public ZoneTransform Transform = new();
    public bool Controlled;
    public float Shield;
    public bool IsDeath;
    private readonly ObservableList<ImmutableList<ConstEffectInfo>> _constEffects = [new List<ConstEffectInfo>().ToImmutableList()];
    public Key Key;
    public Dictionary<BuffType, float> Buffs = new();
    public float Health;
    public float Forcefield;
    public float CreationTime;
    public bool IsCommonMovementActive;
    public float Resource;
    public uint? PlayerId;
    public Key SkinKey = Key.None;
    public Key? AbilityKey;
    public int AbilityCharges;
    public long AbilityChargeCooldownEnd;
    public bool IsRecall;
    public float RecallDuration;
    public ulong RecallEnd;
    public List<GearData> Gears = [];
    public int CurrentGearIndex = -1;
    public Dictionary<int, DeviceData> Devices = new();
    public Dictionary<Key, int> DeviceLevels = new();
    private int? currentDeviceSlot;
    public BuildInfo? CurrentBuildInfo;
    public float ReloadStart;
    public float ReloadEnd;
    public bool IsReloading;
    public float SwitchingGearStart;
    public float SwitchingGearEnd;
    public bool IsSwitchingGear;
    public float DeviceBuildingStart;
    public float DeviceBuildingEnd;
    public bool IsDeviceBuilding;
    public bool IsDeviceBuildingProjectile;
    public bool IsToolLockRotation;
    public bool IsToolLockMovement;
    public bool IsShooting;
    public bool IsBlockClimb;
    public uint? OwnerPlayerId;
    public uint? TurretTargetId = 0U;
    public TeslaChargeType TeslaCharge = TeslaChargeType.NoCharge;
    public PortalLink PortalLinked = new();
    public ulong? BombTimoutEnd;
    public float? UnitProjectileInitSpeed;
    public List<uint> DamageCapturers = [];
    public float CapturePoints = 0.5f;
    public List<Vector3s> CloudAffectedBlocks = [];
    
    public IServiceZone? ZoneService { get; set; }

    public ImmutableDictionary<Key, ulong?> InitialEffects { get; set; }

    public ImmutableList<ConstEffectInfo> ActiveEffects
    {
        get => _constEffects[0];
        set => _constEffects[0] = value;
    }
    
    public readonly Dictionary<ConstEffectAura, Unit[]> UnitsInAuraSinceLastUpdate = new(); 
    public readonly Dictionary<ConstEffectAura, IBoundingShape> AuraEffects = new();
    public readonly Dictionary<ConstEffectOnNearbyBlock, IBoundingShape> NearbyBlockEffects = new();

    private bool _skipBuffSet;

    public CardUnit? UnitCard => Databases.Catalogue.GetCard<CardUnit>(Key);
    
    public Vector3 GetExactPosition() => GetExactPosition(Transform.Position);

    public Vector3 GetExactPosition(Vector3 position) => Vector3.Lerp(position,
        position + Transform.GetLocalVelocity(),
        Math.Clamp(((ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds() - LastMoveUpdateTime) / 1000f,
            0, 1));

    public bool IsBuff(BuffType buff) => Buffs.ContainsKey(buff);

    public float GetBuff(BuffType buff, float def = 0)
    {
        return Buffs.GetValueOrDefault(buff, def);
    }

    private bool IsImmune(ConstEffectInfo effect)
    {
        var immunities = ActiveEffects.Select(info => info.Card.Effect).OfType<ConstEffectImmunity>();
        
        var eCard = effect.Card;
        foreach (var immunity in immunities)
        {
            if (immunity.EffectLabels?.Intersect(eCard.Labels ?? []).Any() ?? false) return true;
            if (immunity.EffectKeys?.Contains(effect.Key) ?? false) return true;
        }
        
        return false;
    }

    private bool ContainsLabelOrIsUnitType(ConstEffectInfo effect)
    {
        var eCard = effect.Card;
        if (UnitCard is not { } uCard) return false;
        if (eCard.Effect?.Targeting?.AffectedLabels is { } labels&& labels.Count != 0 && (!uCard.Labels?.Intersect(labels).Any() ?? true)) return false;
        return eCard.Effect?.Targeting?.AffectedUnits is not { } units || units.Count == 0 ||
               units.Exists(u => u == uCard.Data?.Type);
    }

    private bool DoesEffectApply(ConstEffectInfo effect, TeamType sourceTeam) =>
        effect.Card.Effect?.Targeting?.AffectedTeam switch
        {
            RelativeTeamType.Friendly when sourceTeam != Team => false,
            RelativeTeamType.Opponent when sourceTeam == Team => false,
            _ => ContainsLabelOrIsUnitType(effect)
        };

    public void AddEffect(ConstEffectInfo effect, TeamType sourceTeam)
    {
        if(ActiveEffects.Contains(effect)) return;
        
        if(!DoesEffectApply(effect, sourceTeam) || IsImmune(effect)) return;
        
        ActiveEffects = ActiveEffects.Add(effect);
        
        _updater.OnUnitUpdate(this, new UnitUpdate
        {
            Buffs = Buffs,
            Effects = ActiveEffects.ToInfoDictionary()
        });
    }

    public void AddEffects(ICollection<ConstEffectInfo> effects, TeamType sourceTeam)
    {
        var actualEffects = effects.Where(e => !ActiveEffects.Contains(e) && DoesEffectApply(e, sourceTeam) && !IsImmune(e)).ToList();
        if (actualEffects.Count == 0) return;
        ActiveEffects = ActiveEffects.AddRange(actualEffects);
        
        _updater.OnUnitUpdate(this, new UnitUpdate
        {
            Buffs = Buffs,
            Effects = ActiveEffects.ToInfoDictionary()
        });
    }

    public void RemoveEffect(ConstEffectInfo effect, TeamType sourceTeam)
    {
        if(!ActiveEffects.Contains(effect)) return;
        
        if (!DoesEffectApply(effect, sourceTeam)) return;
        
        ActiveEffects = ActiveEffects.Remove(effect);
        
        _updater.OnUnitUpdate(this, new UnitUpdate
        {
            Buffs = Buffs,
            Effects = ActiveEffects.ToInfoDictionary()
        });
    }

    public void RemoveEffects(ICollection<ConstEffectInfo> effects, TeamType sourceTeam)
    {
        var actualEffects = effects.Where(e => ActiveEffects.Contains(e) && DoesEffectApply(e, sourceTeam)).ToList();
        if (actualEffects.Count == 0) return;
        ActiveEffects = ActiveEffects.RemoveRange(actualEffects);
        
        _updater.OnUnitUpdate(this, new UnitUpdate
        {
            Buffs = Buffs,
            Effects = ActiveEffects.ToInfoDictionary()
        });
    }

    public void RemoveExpiredEffects()
    {
        ActiveEffects = ActiveEffects.RemoveAll(effect => effect.IsExpired);
        _updater.OnUnitUpdate(this, new UnitUpdate
        {
            Buffs = Buffs,
            Effects = ActiveEffects.ToInfoDictionary()
        });
    }

    private void CreateIntervalUpdater(Key constKey, ConstEffect effect)
    {
        if (ActiveEffects.Exists(e => e.Key == constKey)) return;
        switch (effect)
        {
            case ConstEffectAura aura:
                if (aura.IntervalEffects is not { } auraEffects) return;
                _ = new IntervalUpdater(aura.Interval, 
                    auraEffects, 
                    () => ActiveEffects.Exists(e => e.Key == constKey),
                    () =>
                    {
                        UnitsInAuraSinceLastUpdate.TryGetValue(aura, out var units);
                        return units ?? [];
                    },
                    (units, instEffects) => _updater.OnApplyInstEffect(this, units, instEffects, true, CreateImpactData()));
                break;
            case ConstEffectInterval interval:
                if (interval.IntervalEffects is not { } intervalEffects) return;
                _ = new IntervalUpdater(interval.Interval, 
                    intervalEffects, 
                    () => ActiveEffects.Exists(e => e.Key == constKey),
                    () => [this],
                    (units, instEffects) => _updater.OnApplyInstEffect(this, units, instEffects, true, CreateImpactData()));
                break;
            case ConstEffectSelf self:
                if (self.IntervalEffects is not { } selfEffects) return;
                _ = new IntervalUpdater(self.Interval, 
                    selfEffects, 
                    () => ActiveEffects.Exists(e => e.Key == constKey),
                    () => [this],
                    (units, instEffects) => _updater.OnApplyInstEffect(this, units, instEffects, true, CreateImpactData()));
                break;
            default:
                return;
        }
    }

    public bool IsHealth => UnitCard?.Health != null;

    public bool IsForcefield => UnitCard?.Health is { Forcefield: not null };

    public bool IsDead => IsHealth && Health <= 0;

    public bool IsInsideUnit(Vector3s blockPos) => UnitSizeHelper.IsInsideUnit(blockPos, this);

    public CardSkin? SkinCard => Databases.Catalogue.GetCard<CardSkin>(SkinKey);

    public CardAbility? AbilityCard =>
        AbilityKey.HasValue ? Databases.Catalogue.GetCard<CardAbility>(AbilityKey.Value) : null;

    public void StartRecall(float duration, ulong endTime)
    {
        IsRecall = true;
        RecallDuration = duration;
        RecallEnd = endTime;
    }

    public void EndRecall() => IsRecall = false;

    public Key CurrentGearKey
    {
        get
        {
            var gearByIndex = GetGearByIndex(CurrentGearIndex);
            return gearByIndex?.Key ?? Key.None;
        }
    }

    public GearData? CurrentGear => GetGearByIndex(CurrentGearIndex);

    public GearData? GetGearByIndex(int index)
    {
        return index >= 0 && index < Gears.Count ? Gears[index] : null;
    }

    public GearData? GetGearByKey(Key gearKey)
    {
        return Gears.Find((Predicate<GearData>)(i => i.Key == gearKey));
    }

    public int GearKeyToIndex(Key gearKey)
    {
        for (var index = 0; index < Gears.Count; ++index)
        {
            if (Gears[index].Key == gearKey)
                return index;
        }

        return -1;
    }

    public void SetGear(Key gearKey)
    {
        if (gearKey == CurrentGearKey) return;
        UpdateData(new UnitUpdate
        {
            CurrentGear = gearKey
        });
        
        var switchEffects = ActiveEffects.GetEffectsOfType<ConstEffectOnGearSwitch>();
        foreach (var switchEffect in switchEffects.Select(e => e.Effect).OfType<InstEffect>())
        {
            _updater.OnApplyInstEffect(this, [this], switchEffect, false, CreateImpactData());
        }
    }

    public UnitDataPlayer? PlayerUnitData => UnitCard?.Data as UnitDataPlayer;

    public UnitDataTurret? TurretUnitData => UnitCard?.Data as UnitDataTurret;

    public UnitDataMortar? MortarUnitData => UnitCard?.Data as UnitDataMortar;

    public UnitDataLandmine? LandmineUnitData => UnitCard?.Data as UnitDataLandmine;

    public UnitDataBomb? BombUnitData => UnitCard?.Data as UnitDataBomb;

    public UnitDataPickup? PickupUnitData => UnitCard?.Data as UnitDataPickup;

    public UnitDataProjectile? ProjectileUnitData => UnitCard?.Data as UnitDataProjectile;

    public UnitDataCloud? CloudUnitData => UnitCard?.Data as UnitDataCloud;

    public UnitDataSkybeam? SkybeamUnitData => UnitCard?.Data as UnitDataSkybeam;

    public UnitDataTeslaCoil? TeslaUnitData => UnitCard?.Data as UnitDataTeslaCoil;

    public UnitDataShower? ShowerUnitData => UnitCard?.Data as UnitDataShower;

    public UnitDataDrill? DrillUnitData => UnitCard?.Data as UnitDataDrill;

    public ulong LastMoveUpdateTime;
    private ulong _lastUpdateTime;
    
    private readonly UnitUpdater _updater;
    
    private bool IsNewUpdate(ulong updateTime)
    {
        if (_lastUpdateTime >= updateTime) return false;
        _lastUpdateTime = updateTime;
        return true;
    }
    
    private bool IsNewMoveUpdate(ulong updateTime)
    {
        return LastMoveUpdateTime < updateTime;
    }
    
    public Unit(uint id, UnitInit unitInit, UnitUpdater updater)
    { 
        Id = id;
        Key = unitInit.Key;
        if (unitInit.Transform != null) 
            Transform = unitInit.Transform;
        Controlled = unitInit.Controlled;
        OwnerPlayerId = unitInit.OwnerId;
        Team = unitInit.Team;
        PlayerId = unitInit.PlayerId;
        if (unitInit.SkinKey.HasValue)
            SkinKey = unitInit.SkinKey.Value;
        if (unitInit.Gears != null)
            Gears = unitInit.Gears.Select((key, index) => new GearData(this, key, index)).ToList();

        _constEffects.CollectionChanged += OnEffectsChanged;

        if (PlayerId != null)
        {
            Stats = new Dictionary<ScoreType, float>();
        }
        
        var effects = new Dictionary<Key, ulong?>();
        if (UnitCard is { } uCard)
        {
            if (uCard.InitEffects != null)
            {
                foreach (var initEffect in uCard.InitEffects) 
                {
                    effects.Add(initEffect, Databases.Catalogue.GetCard<CardEffect>(initEffect)?.Duration is { } dur
                        ? (ulong)DateTimeOffset.Now.AddSeconds(dur).ToUnixTimeMilliseconds()
                        : null);            
                }
            }

            if (uCard.EnabledEffects != null)
            {
                foreach (var enabledEffect in uCard.EnabledEffects)
                {
                    effects.Add(enabledEffect, Databases.Catalogue.GetCard<CardEffect>(enabledEffect)?.Duration is { } dur
                        ? (ulong)DateTimeOffset.Now.AddSeconds(dur).ToUnixTimeMilliseconds()
                        : null);
                }
            }
        }
        
        InitialEffects = effects.ToImmutableDictionary();
        
        _updater = updater;
        _updater.OnUnitInit(this, unitInit);
    }

    public UnitInit GetInitData()
    {
        var newUnit = new UnitInit
        {
            Key = Key,
            Transform = Transform,
            Controlled = Controlled,
            OwnerId = OwnerPlayerId,
            Team = Team,
            PlayerId = PlayerId
        };
        
        if (SkinKey != Key.None)
        {
            newUnit.SkinKey = SkinKey;
        }

        if (Gears.Count > 0)
        {
            newUnit.Gears = Gears.Select(gear => gear.Key).ToList();
        }
        
        return newUnit;
    }

    public void UpdateData(UnitUpdate data, ulong? updateTime = null)
    {
        if (updateTime.HasValue && !IsNewUpdate(updateTime.Value)) return;
        
        var effectsBeforeUpdate = ActiveEffects;
        var buffsBeforeUpdate = Buffs;
        
        if (data.Buffs != null)
        {
            Buffs = data.Buffs;
            _skipBuffSet = true;
        }
        
        if (data.Effects != null)
        {
            ActiveEffects = ConstEffectInfo.Convert(data.Effects);
        }
        
        if (data.Ammo != null)
        {
            foreach (var keyValuePair in data.Ammo)
            {
                var gearByKey = GetGearByKey(keyValuePair.Key);
                gearByKey?.ServerUpdateAmmo(keyValuePair.Value);
            }
        }

        if (data.Health.HasValue)
        {
            var wasDead = IsDead;
            var oldHealth = Health;
            Health = data.Health.Value;

            if (!wasDead && IsDead)
            {
                var deathEffects = ActiveEffects.GetEffectsOfType<ConstEffectOnDeath>();
                foreach (var effect in deathEffects.Select(dth => dth.Effect).OfType<InstEffect>())
                {
                    _updater.OnApplyInstEffect(this, [this], effect, false, CreateImpactDataExact());
                } 
            }
            else if (UnitCard?.Health?.Health is not null)
            {
                var lowHealthEffects = ActiveEffects.GetEffectsOfType<ConstEffectOnLowHealth>();
                var maxHp = this.UnitMaxHealth(UnitCard.Health.Health.MaxHealth);
                if (maxHp > 0)
                {
                    var oldHealthPercentage = oldHealth / maxHp;
                    var currHealthPercentage = Health / maxHp;
                    foreach (var effect in lowHealthEffects)
                    {
                        if (currHealthPercentage <= effect.HealthThreshold && oldHealthPercentage > effect.HealthThreshold)
                        {
                            if (effect.OnThresholdDown is { } thresholdDown)
                            {
                                _updater.OnApplyInstEffect(this, [this], thresholdDown, false, CreateImpactData());
                            }

                            if (effect.ConstantEffects is { } constantEffects)
                            {
                                AddEffects(constantEffects.Select(k => new ConstEffectInfo(k)).ToList(), Team);
                            }
                        }
                        else if (currHealthPercentage > effect.HealthThreshold &&
                                 oldHealthPercentage <= effect.HealthThreshold && 
                                 effect.ConstantEffects is { } constantEffects)
                        {
                            RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)).ToList(), Team);
                        }
                    }
                }
            }
        }
            
        if (data.CurrentGear.HasValue)
            CurrentGearIndex = GearKeyToIndex(data.CurrentGear.Value);
        if (data.CapturePoints.HasValue)
            CapturePoints = data.CapturePoints.Value;
        if (data.Team.HasValue)
            Team = data.Team.Value;
        if (data.TurretTargetId.HasValue)
            TurretTargetId = data.TurretTargetId.Value != 0U ? data.TurretTargetId.Value : null;
        if (data.Resource.HasValue)
            Resource = data.Resource.Value;
        
        if (data.Devices != null)
        {
            foreach (var device in data.Devices)
                Devices[device.Key] = device.Value;
        }

        if (data.Forcefield.HasValue)
            Forcefield = data.Forcefield.Value;
        if (data.Shield.HasValue)
            Shield = data.Shield.Value;
        if (data.AbilityCharges.HasValue)
            AbilityCharges = data.AbilityCharges.Value;
        if (data.AbilityChargeCooldownEnd.HasValue)
            AbilityChargeCooldownEnd = (long)data.AbilityChargeCooldownEnd.Value;
        if (data.Ability.HasValue)
            AbilityKey = data.Ability.Value;
        if (data.ProjectileInitSpeed.HasValue)
            UnitProjectileInitSpeed = data.ProjectileInitSpeed.Value;
        if (data.BombTimeoutEnd.HasValue)
            BombTimoutEnd = data.BombTimeoutEnd.Value != 0UL ? data.BombTimeoutEnd.Value : null;
        if (data.CloudAffectedBlocks != null)
            CloudAffectedBlocks = data.CloudAffectedBlocks;
        if (data.MovementActive.HasValue)
            IsCommonMovementActive = data.MovementActive.Value;
        if (data.DamageCapturers != null)
            DamageCapturers = data.DamageCapturers;
        if (data.PortalLink != null)
            PortalLinked = data.PortalLink;
        if (data.TeslaCharge.HasValue)
            TeslaCharge = data.TeslaCharge.Value;

        if (!effectsBeforeUpdate.SequenceEqual(ActiveEffects))
        {
            data.Effects = ActiveEffects.ToInfoDictionary();
        }

        if (buffsBeforeUpdate != Buffs)
        {
            data.Buffs ??= Buffs;
        }
        
        _skipBuffSet = false;
            
        _updater.OnUnitUpdate(this, data);
    }

    public UnitUpdate GetUpdateData()
    {
        var newUpdate = new UnitUpdate();
        if (Gears.Count > 0)
        {
            var gearMap = Gears.ToDictionary(gear => gear.Key);
            newUpdate.Ammo = [];
            foreach (var gear in gearMap)
            {
                var ammo = gear.Value.Ammo.Select(ammo => new Ammo { Index = ammo.AmmoIndex, Mag = ammo.Mag, Pool = ammo.Pool }).ToList();
                newUpdate.Ammo.Add(gear.Key, ammo);
            }
            
            newUpdate.CurrentGear = GetGearByIndex(CurrentGearIndex)?.Key;
        }

        if (UnitCard?.Health?.Health != null)
        {
            newUpdate.Health = Health;
            newUpdate.Shield = Shield;
        }

        if (UnitCard?.Health?.Forcefield != null)
        {
            newUpdate.Forcefield = Forcefield;
        }

        if (CapturePoints > 0)
        {
            newUpdate.CapturePoints = CapturePoints;
        }
        
        newUpdate.Team = Team;
        
        if (Resource > 0)
        {
            newUpdate.Resource = Resource;
        }
        
        newUpdate.Buffs = Buffs;
        newUpdate.Effects = ActiveEffects.ToInfoDictionary();

        if (UnitCard?.Data is UnitDataPlayer)
        {
            newUpdate.Devices = Devices;
            newUpdate.Ability = AbilityKey;
            newUpdate.AbilityCharges = AbilityCharges;
            newUpdate.AbilityChargeCooldownEnd = (ulong) AbilityChargeCooldownEnd;
            newUpdate.MovementActive = IsCommonMovementActive;
        }

        if (UnitCard?.Data is UnitDataTurret)
        {
            newUpdate.TurretTargetId = TurretTargetId;
        }

        if (UnitCard?.Data is UnitDataProjectile)
        {
            newUpdate.ProjectileInitSpeed = UnitProjectileInitSpeed;
        }

        if (UnitCard?.Data is UnitDataBomb)
        {
            newUpdate.BombTimeoutEnd = BombTimoutEnd;
        }

        if (UnitCard?.Data is UnitDataCloud)
        {
            newUpdate.CloudAffectedBlocks = CloudAffectedBlocks;
        }

        if (UnitCard?.Data is UnitDataPortal)
        {
            newUpdate.PortalLink = PortalLinked;
        }

        if (UnitCard?.Data is UnitDataTeslaCoil)
        {
            newUpdate.TeslaCharge = TeslaCharge;
        }

        if (DamageCapturers.Count > 0)
        {
            newUpdate.DamageCapturers = DamageCapturers;
        }
        
        return newUpdate;
    }

    public bool UnitMove(ZoneTransform transform, ulong moveTime)
    {
        if(!IsNewMoveUpdate(moveTime) || IsDead) return true;

        var wasSprinting = Transform.IsSprint;
        
        LastMoveUpdateTime = moveTime;
        Transform = transform; 
        foreach (var aura in AuraEffects)
        {
            AuraEffects[aura.Key] = aura.Value switch
            {
                BoundingSphere sphere => new BoundingSphere(transform.Position, sphere.Radius),
                BoundingEllipsoid ellipsoid => new BoundingEllipsoid(transform.Position, ellipsoid.XRadius,
                    ellipsoid.YRadius, ellipsoid.ZRadius),
                _ => AuraEffects[aura.Key]
            };
        }

        foreach (var nearby in NearbyBlockEffects)
        {
            NearbyBlockEffects[nearby.Key] = nearby.Value switch
            {
                BoundingSphere sphere => new BoundingSphere(transform.Position, sphere.Radius),
                BoundingEllipsoid ellipsoid => new BoundingEllipsoid(transform.Position, ellipsoid.XRadius,
                    ellipsoid.YRadius, ellipsoid.ZRadius),
                _ => NearbyBlockEffects[nearby.Key]
            };
        }
        
        if (!wasSprinting && transform.IsSprint)
        {
            var sprintEffects = ActiveEffects.GetEffectsOfType<ConstEffectOnSprint>();
            foreach (var sprintEffect in sprintEffects)
            {
                if (sprintEffect.ConstantEffects is { } constantEffects)
                {
                    AddEffects(constantEffects.Select(k => new ConstEffectInfo(k)).ToList(), Team);
                }
            }
        }
        else if (wasSprinting && !transform.IsSprint)
        {
            var sprintEffects = ActiveEffects.GetEffectsOfType<ConstEffectOnSprint>();
            foreach (var sprintEffect in sprintEffects)
            {
                if (sprintEffect.ConstantEffects is { } constantEffects)
                {
                    RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)).ToList(), Team);
                }
            }
        }
        
        _updater.OnUnitMove(this, moveTime, transform);
        return true;
    }

    public void ReloadAmmo()
    {
        var updatedAmmo = new List<Ammo>();
        if (CurrentGear?.Tools is null) return;
        foreach (var tool in CurrentGear.Tools.Where(tool => tool.IsPossibleToReload()))
        {
            if (tool.GetAmmoData() is not { } ammoData) continue;
            switch (CurrentGear.Card.Reload)
            {
                case ReloadPartial reloadPartial:
                    var newPartialAmmo = ammoData.ReloadPartialAmmo(reloadPartial.ReloadRate);
                    if (newPartialAmmo is not null)
                        updatedAmmo.Add(newPartialAmmo);
                    break;
                default:
                    var newAmmo = ammoData.ReloadAmmo();
                    if (newAmmo is not null)
                        updatedAmmo.Add(newAmmo);
                    break;
            }
        }
        
        if (updatedAmmo.Count > 0)
        {
            UpdateData(new UnitUpdate
            {
                Ammo = new Dictionary<Key, List<Ammo>> { {CurrentGear.Key, updatedAmmo} }
            });
        }
    }

    private Dictionary<BuffType, float> ExtractBuffs(IEnumerable<Key> effects)
    {
        var buffResult = new Dictionary<BuffType, float>();
        foreach (var card in effects.Select(effect => Databases.Catalogue.GetCard<CardEffect>(effect)))
        {
            if (card?.Effect is not ConstEffectBuff { Buffs: not null } effectBuff) continue;
            foreach (var buff in effectBuff.Buffs)
            {
                if (buffResult.TryGetValue(buff.Key, out var value))
                {
                    buffResult[buff.Key] = this.CombineBuffs(value, buff.Value, buff.Key, card.Key);
                }
                else
                {
                    buffResult.Add(buff.Key, buff.Value);
                }
            }
        }
        
        return buffResult;
    }

    private void OnEffectsChanged(in NotifyCollectionChangedEventArgs<ImmutableList<ConstEffectInfo>> e)
    {
        if (!e.IsSingleItem) return;
        var addedItems = e.NewItem.Except(e.OldItem);
        var removedItems = e.OldItem.Except(e.NewItem);
        var removed = removedItems.Select(info => (Databases.Catalogue.GetCard<CardEffect>(info.Key), info)).ToList();
        var added = addedItems.Select(info => (Databases.Catalogue.GetCard<CardEffect>(info.Key), info)).ToList();
        if (added.Count == 0 && removed.Count == 0) return;
        
        if (!_skipBuffSet && (removed.Exists(eff => eff.Item1?.Effect is ConstEffectBuff) ||
                              added.Exists(eff => eff.Item1?.Effect is ConstEffectBuff)))
        {
            Buffs = ExtractBuffs(e.NewItem.Select(info => info.Key).Distinct());
            if (!_updater.DoesObjBuffApply(Team, UnitCard?.Labels ?? []))
            {
                Buffs.Remove(BuffType.Invulnerability);
            }
        }
        
        var center = Transform.Position;
        foreach (var (effect, info) in removed)
        {
            switch (effect?.Effect)
            {
                case ConstEffectAura aura:
                    AuraEffects.Remove(aura);
                    UnitsInAuraSinceLastUpdate.Remove(aura, out var affectedUnits);
                    if (affectedUnits != null)
                    {
                        var constEffects = aura.ConstantEffects?.Select(k => new ConstEffectInfo(k, null)).ToList() ?? [];
                        foreach (var affectedUnit in affectedUnits)
                        {
                            affectedUnit.RemoveEffects(constEffects, Team);
                        }
                    }
                    break;
                case ConstEffectOnNearbyBlock nearby:
                    NearbyBlockEffects.Remove(nearby);
                    if (nearby.Effects is { Count: > 0 } blockEffects)
                        RemoveEffects(blockEffects.Select(k => new ConstEffectInfo(k, null)).ToList(), Team);
                    break;
                case ConstEffectSelf constEffectSelf:
                    if (constEffectSelf.ConstantEffects is { Count: > 0 } effects)
                    {
                        RemoveEffects(effects.Select(k => new ConstEffectInfo(k, null)).ToList(), Team);
                    }
                    break;
                case ConstEffectTeam:
                    _updater.OnTeamEffectRemoved(this, info);
                    break;
            }
        }
        
        foreach (var (effect, info) in added)
        {
            switch (effect?.Effect)
            {
                case ConstEffectAura { InnerRadius: not null } aura when !(Math.Abs(aura.InnerRadius.Value - aura.OuterRadius) < 0.0001):
                    AuraEffects.Add(aura, new BoundingEllipsoid(center, aura.OuterRadius, aura.InnerRadius.Value));
                    CreateIntervalUpdater(info.Key, aura);
                    break;
                case ConstEffectAura aura:
                    AuraEffects.Add(aura, new BoundingSphere(center, aura.OuterRadius));
                    CreateIntervalUpdater(info.Key, aura);
                    break;
                case ConstEffectInterval constEffectInterval:
                    CreateIntervalUpdater(info.Key, constEffectInterval);
                    break;
                case ConstEffectOnNearbyBlock nearby:
                    NearbyBlockEffects.Add(nearby, new BoundingSphere(center, nearby.Radius));
                    break;
                case ConstEffectSelf constEffectSelf:
                    CreateIntervalUpdater(info.Key, constEffectSelf);
                    if (constEffectSelf.ConstantEffects is { Count: > 0 } effects)
                        AddEffects(effects.Select(k => new ConstEffectInfo(k)).ToList(), Team);
                    break;
                case ConstEffectTeam:
                    _updater.OnTeamEffectAdded(this, info);
                    break;
            }
        }
    }
}