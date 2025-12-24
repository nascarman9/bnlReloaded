using System.Collections.Immutable;
using System.Numerics;
using BNLReloadedServer.Database;
using BNLReloadedServer.Octree_Extensions;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Service;
using ObservableCollections;

namespace BNLReloadedServer.BaseTypes;

public partial class Unit
{
    public uint Id;
    public TeamType Team;
    public ZoneTransform Transform = new();
    public bool Controlled;
    private float _shield;
    private readonly ObservableList<ImmutableList<ConstEffectInfo>> _constEffects = [new List<ConstEffectInfo>().ToImmutableList()];
    private readonly Dictionary<Key, HashSet<EffectSource>> _effectSources = new();
    public Key Key;
    private Dictionary<BuffType, float> _buffs = new();
    private float _health;
    private float _forcefield;
    private bool _isCommonMovementActive;
    public float Resource;
    public uint? PlayerId;
    public readonly Key SkinKey = Key.None;
    public Key? AbilityKey;
    public int AbilityCharges;
    private long _abilityChargeCooldownEnd;
    public uint? SpawnId;
    public bool IsRecall;
    public bool IsDead = false;
    public bool IsActive = true;
    public bool IsFirstLand = true;
    public bool WasAfkWarned;
    public readonly List<GearData> Gears = [];
    private int _currentGearIndex = -1;
    public readonly Dictionary<int, DeviceData> Devices = new();
    public Dictionary<Key, int> DeviceLevels = new();
    public BuildInfo? CurrentBuildInfo;
    public uint? OwnerPlayerId;
    public uint? TurretTargetId = 0U;
    public TeslaChargeType TeslaCharge = TeslaChargeType.NoCharge;
    private int _charges;
    public PortalLink PortalLinked = new();
    private DateTimeOffset? _bombTimeoutEnd;
    private float? _unitProjectileInitSpeed;
    private List<uint> _damageCapturers = [];
    private float _capturePoints = 0.5f;
    public List<Vector3s> CloudAffectedBlocks = [];
    public HashSet<Vector3s> OverlappingMapBlocks = [];
    public Action? OnDestroyed;
    public bool LastDashChargeMax = false;
    public bool AbilityTriggered = false;
    public Vector3s? AttachedTo = null;
    
    public DateTimeOffset CreationTime;
    private readonly DateTimeOffset? _expirationTime;
    private DateTimeOffset? _rechargeForcefieldTime;
    public DateTimeOffset? StartChargeTime;
    public DateTimeOffset? TimeTillNextAbilityCharge;
    public DateTimeOffset? AbilityTriggerTimeEnd;
    public DateTimeOffset? RespawnTime;
    public DateTimeOffset? RecallTime;
    public DateTimeOffset? SpawnProtectionTime;
    public DateTimeOffset? LastMoveTime;

    public ulong TicksPerChannel = 0;
    public ChannelData? CurrentChannelData;

    public InstEffect? OnMortarHit;

    public int HitCount = 0;
    
    public bool JustTeleported = false;
    public DateTimeOffset? LastTeleport;
    
    public bool IsDropped;
    public bool CanPickUp = true;

    private bool _wasDisabled;
    private DateTimeOffset? _disabledTime;
    private bool _wasConfused;
    private bool _everConfused;
    private bool _wasDisarmed;
    
    private uint? PermaOwnerPlayerId { get; }
    private TeamType PermaTeam { get; }

    private Dictionary<ConstEffectInfo, PersistOnDeathSource>? _returnOnRevive;

    public readonly EffectArea? CloudEffect;
    public readonly EffectArea? DamageCaptureEffect;
    public readonly EffectArea? LandmineEffect;
    
    public readonly Dictionary<Unit, DateTimeOffset> RecentDamagers = new();

    private float _minPullForce;
    private DateTimeOffset? _lastPullTime;
    private uint? _activePuller;
    
    public TimeSpan TimeSinceCreated => DateTimeOffset.Now - CreationTime;

    public bool IsFuseExpired => _bombTimeoutEnd.HasValue && !IsBuff(BuffType.Disabled) &&
                                 DateTimeOffset.Now >= _bombTimeoutEnd;
    
    public bool IsExpired => _expirationTime.HasValue && DateTimeOffset.Now >= _expirationTime;

    public float LengthOfCharge =>
        StartChargeTime is null ? 0.0f : (float)(DateTimeOffset.Now - StartChargeTime.Value).TotalSeconds; 
    
    public bool IsNewAbilityChargeReady =>
        TimeTillNextAbilityCharge is not null && DateTimeOffset.Now >= TimeTillNextAbilityCharge;

    public bool IsTriggerTimeUp => AbilityTriggered && (AbilityTriggerTimeEnd is null || DateTimeOffset.Now >= AbilityTriggerTimeEnd);

    public bool DoRecall => IsRecall && RecallTime is not null && RecallTime < DateTimeOffset.Now;
    
    public bool CanTeleport => !JustTeleported && (LastTeleport is null || DateTimeOffset.Now - LastTeleport > TimeSpan.FromSeconds(1));

    public bool HasSpawnProtection => SpawnProtectionTime is not null && SpawnProtectionTime > DateTimeOffset.Now;
    
    public IServiceZone? ZoneService { get; set; }

    public ImmutableDictionary<Key, ulong?> InitialEffects { get; set; }

    public ImmutableList<ConstEffectInfo> ActiveEffects
    {
        get => _constEffects[0];
        set => _constEffects[0] = value;
    }

    private UnitSource SelfSource { get; }
    
    public readonly Dictionary<ConstEffectAura, Unit[]> UnitsInAuraSinceLastUpdate = new(); 
    public readonly Dictionary<ConstEffectAura, IBoundingShape> AuraEffects = new();
    public readonly Dictionary<ConstEffectOnNearbyBlock, IBoundingShape> NearbyBlockEffects = new();

    public TeamType WinningTeam = TeamType.Neutral;
    public readonly List<ConstEffectOnMatchContext> MatchContextEffects = [];

    private bool _skipBuffSet;

    public CardUnit? UnitCard => Databases.Catalogue.GetCard<CardUnit>(Key);

    public UnitSource GetSelfSource(ImpactData? impactData = null) =>
        impactData == null ? SelfSource : new UnitSource(this, impactData);

    public Vector3 GetMidpoint() => GetMidpoint(Transform.Position);
    
    public Vector3 GetMidpoint(Vector3 unitPos) =>
        UnitCard is { Size: not null } uCard ? uCard.PivotType switch
        {
            UnitPivotType.Zero => unitPos + uCard.Size.Value.ToVector3() / 2,
            UnitPivotType.Center => unitPos,
            UnitPivotType.CenterBottom or
            UnitPivotType.PointBottom => unitPos with
            {
                Y = unitPos.Y + (PlayerId != null
                    ? Transform.IsCrouch ? 0.45f : 0.95f
                    : uCard.Size.Value.ToVector3().Y / 2)
            },
            _ => unitPos
        } : unitPos;
    
    public Vector3 GetFallPosition() => 
        UnitCard is { Size: not null } uCard ? uCard.PivotType switch
        {
            UnitPivotType.Zero => Transform.Position + uCard.Size.Value.ToVector3() with { Y = 0 } / 2,
            UnitPivotType.Center => Transform.Position with
            {
                Y = Transform.Position.Y - uCard.Size.Value.ToVector3().Y / 2
            },
            _ => Transform.Position
        } : Transform.Position;

    public Vector3 GetExactPosition() => GetExactPosition(Transform.Position);

    public Vector3 GetExactPosition(Vector3 position) => Vector3.Lerp(position,
        position + Transform.GetLocalVelocity(),
        Math.Clamp(((ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds() - LastMoveUpdateTime) / 1000f,
            0, 1));
    
    private Vector3 GetExactPositionInFuture(float futureSeconds) => GetExactPositionInFuture(Transform.Position, futureSeconds);
    
    private Vector3 GetExactPositionInFuture(Vector3 position, float futureSeconds) => Vector3.Lerp(position,
        position + Transform.GetLocalVelocity(),
        Math.Clamp(((ulong)DateTimeOffset.Now.AddSeconds(futureSeconds).ToUnixTimeMilliseconds() - LastMoveUpdateTime) / 1000f,
            0, 1));

    public IEnumerable<Vector3> GetPositionSteps(uint stepCount, bool extraStep = false)
    {
        if (stepCount <= 0 || Transform.LocalVelocity == Vector3s.Zero)
        {
            yield return Transform.Position;
            yield break;
        }
        
        var initialPosition = Transform.Position;
        var exactPosition = GetExactPosition();
        var step = (exactPosition - initialPosition) / stepCount;
        for (var i = 0; i <= stepCount; i++)
        {
            yield return initialPosition + i * step;
        }

        if (extraStep)
        {
            yield return GetExactPositionInFuture(0.1f);
        }
    }

    public bool IsBuff(BuffType buff) => _buffs.ContainsKey(buff);

    public float GetBuff(BuffType buff, float def = 0) => _buffs.GetValueOrDefault(buff, def);

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

    private bool ContainsLabelOrIsUnitType(EffectTargeting targeting)
    {
        if (UnitCard is not { } uCard) return false;
        if (targeting.AffectedLabels is { } labels && labels.Count != 0 &&
            (!uCard.Labels?.Intersect(labels).Any() ?? true)) return false;
        return targeting.AffectedUnits is not { } units || units.Count == 0 ||
               units.Exists(u => u == uCard.Data?.Type);
    }
    
    public bool DoesEffectApply(ConstEffectInfo effect) =>
        effect.Card.Effect?.Targeting is not { } targeting || ContainsLabelOrIsUnitType(targeting);

    public bool DoesEffectApply(ConstEffectInfo effect, TeamType sourceTeam) =>
        effect.Card.Effect?.Targeting is not { } targeting || DoesEffectApply(targeting, sourceTeam);

    public bool DoesEffectApply(EffectTargeting targeting, TeamType sourceTeam) =>
        targeting.AffectedTeam switch
        {
            RelativeTeamType.Friendly when sourceTeam != Team => false,
            RelativeTeamType.Opponent when sourceTeam == Team => false,
            _ => ContainsLabelOrIsUnitType(targeting)
        };

    public void AddEffect(ConstEffectInfo effect, TeamType sourceTeam, EffectSource? source)
    {
        if(!DoesEffectApply(effect, sourceTeam) || IsImmune(effect)) return;
        
        if (IsDead && source is PersistOnDeathSource persistOnDeathSource)
        {
            _returnOnRevive?.TryAdd(effect, persistOnDeathSource);
        }
        
        if (IsDead)
        {
            return;
        }
        
        if (effect is { TimestampEnd: not null })
        {
            var existing = ActiveEffects.Find(e => e.Key == effect.Key);

            if (existing is null && source is not null)
            {
                _effectSources.Add(effect.Key, [source]);
            }
            else if (existing?.TimestampEnd is not null && existing.TimestampEnd < effect.TimestampEnd)
            {
                existing.UpdateDuration(effect.TimestampEnd.Value);
                _effectSources.Remove(effect.Key);
                if (source is not null)
                {
                    _effectSources.Add(effect.Key, [source]);
                }

                _updater.OnUnitUpdate(this, new UnitUpdate
                {
                    Buffs = _buffs,
                    Effects = ActiveEffects.ToInfoDictionary()
                });
                return;
            }
        }
        else if (source is not null)
        {
            if (_effectSources.TryGetValue(effect.Key, out var effectSource))
            {
                effectSource.Add(source);
            }
            else
            {
                _effectSources.Add(effect.Key, [source]);
            }
        }
        
        if (source is not null && effect.Card.Effect is ConstEffectBuff)
        {
            var buffer = source.Impact?.CasterPlayerId is not null
                ? _updater.GetPlayerFromPlayerId(source.Impact.CasterPlayerId.Value)
                : null;
            
            BuffStatsUpdate(!effect.Card.Positive, source, buffer);
        }
        
        if(ActiveEffects.Contains(effect)) return;
        
        ActiveEffects = ActiveEffects.Add(effect);
        
        _updater.OnUnitUpdate(this, new UnitUpdate
        {
            Buffs = _buffs,
            Effects = ActiveEffects.ToInfoDictionary()
        });
    }

    public void AddEffects(IEnumerable<ConstEffectInfo> effects, TeamType sourceTeam, EffectSource? source)
    {
        var appliedEffects = effects.Where(e => DoesEffectApply(e, sourceTeam) && !IsImmune(e)).ToList();
        if (appliedEffects.Count == 0) return;

        if (IsDead && source is PersistOnDeathSource persistOnDeathSource)
        {
            foreach (var effect in appliedEffects)
            {
                _returnOnRevive?.TryAdd(effect, persistOnDeathSource);
            }
        }

        if (IsDead)
        {
            return;
        }

        var doUpdate = false;
        
        foreach (var effect in appliedEffects.ToList())
        {
            if (effect is { TimestampEnd: not null })
            {
                var existing = ActiveEffects.Find(e => e.Key == effect.Key);

                if (existing is null && source is not null)
                {
                    if (_effectSources.TryGetValue(effect.Key, out var effectSource))
                    {
                        effectSource.Add(source);
                    }
                    else
                    {
                        _effectSources.Add(effect.Key, [source]);
                    }
                }
                else if (existing?.TimestampEnd is not null && existing.TimestampEnd < effect.TimestampEnd)
                {
                    existing.UpdateDuration(effect.TimestampEnd.Value);
                    _effectSources.Remove(effect.Key);
                    if (source is not null)
                    {
                        if (_effectSources.TryGetValue(effect.Key, out var effectSource))
                        {
                            effectSource.Add(source);
                        }
                        else
                        {
                            _effectSources.Add(effect.Key, [source]);
                        }
                    }
                    doUpdate = true;
                }
                
                if (existing is null) continue;
                
                appliedEffects.Remove(effect);
            }
            else if (source is not null)
            {
                if (_effectSources.TryGetValue(effect.Key, out var effectSource))
                {
                    effectSource.Add(source);
                }
                else
                {
                    _effectSources.Add(effect.Key, [source]);
                }
            }
        }

        var actualEffects = appliedEffects.Where(e => !ActiveEffects.Contains(e)).ToList();

        if (source is not null)
        {
            foreach (var effect in actualEffects.Select(e => e.Card).Where(e => e.Effect is ConstEffectBuff))
            {
                var buffer = source.Impact?.CasterPlayerId is not null
                    ? _updater.GetPlayerFromPlayerId(source.Impact.CasterPlayerId.Value)
                    : null;
                
                BuffStatsUpdate(!effect.Positive, source, buffer);
            }
        }
        
        if (actualEffects.Count == 0)
        {
            if (doUpdate)
            {
                _updater.OnUnitUpdate(this, new UnitUpdate
                {
                    Buffs = _buffs,
                    Effects = ActiveEffects.ToInfoDictionary()
                });
            }

            return;
        }
        
        ActiveEffects = ActiveEffects.AddRange(actualEffects);
        
        _updater.OnUnitUpdate(this, new UnitUpdate
        {
            Buffs = _buffs,
            Effects = ActiveEffects.ToInfoDictionary()
        });
    }

    public void RemoveEffect(ConstEffectInfo effect, TeamType sourceTeam, EffectSource? source, bool clearAll = false)
    {
        if(effect.HasDuration || !ActiveEffects.Contains(effect)) return;
        
        if (!DoesEffectApply(effect, sourceTeam)) return;
        
        if (IsDead)
        {
            return;
        }

        if (clearAll)
        {
            _effectSources.Remove(effect.Key);
        }
        else if (_effectSources.TryGetValue(effect.Key, out var effectSource))
        {
            if (source is not null)
            {
                effectSource.Remove(source);
            }

            if (effectSource.Count > 0) return;
            _effectSources.Remove(effect.Key);
        }

        ActiveEffects = ActiveEffects.Remove(effect);
        
        _updater.OnUnitUpdate(this, new UnitUpdate
        {
            Buffs = _buffs,
            Effects = ActiveEffects.ToInfoDictionary()
        });
    }

    public void RemoveEffects(IEnumerable<ConstEffectInfo> effects, TeamType sourceTeam, EffectSource? source, bool clearAll = false)
    {
        Func<ConstEffectInfo, TeamType, bool> doCheck = _everConfused ? (eff, _) => DoesEffectApply(eff) : DoesEffectApply;
        var actualEffects = effects
            .Where(e => !e.HasDuration && ActiveEffects.Contains(e) && doCheck(e, sourceTeam)).ToList();
        
        if (actualEffects.Count == 0) return;
        
        if (IsDead)
        {
            return;
        }
        
        foreach (var effect in actualEffects.ToList())
        {
            if (clearAll)
            {
                _effectSources.Remove(effect.Key);
                continue;
            }
            
            if (!_effectSources.TryGetValue(effect.Key, out var effectSource)) continue;
            if (source is not null)
            {
                effectSource.Remove(source);
            }
            if (effectSource.Count > 0) 
                actualEffects.Remove(effect);
            else
            {
                _effectSources.Remove(effect.Key);
            }
        }
        
        ActiveEffects = ActiveEffects.RemoveRange(actualEffects);
        
        _updater.OnUnitUpdate(this, new UnitUpdate
        {
            Buffs = _buffs,
            Effects = ActiveEffects.ToInfoDictionary()
        });
    }

    public void RemoveTriggerEffects()
    {
        var removedEffects = ActiveEffects.Where(e =>
                _effectSources.TryGetValue(e.Key, out var effectSources) &&
                effectSources.Any(src => src is TriggerSource))
            .ToList();

        foreach (var effect in removedEffects.ToList())
        {
            if (_effectSources.TryGetValue(effect.Key, out var effectSource))
            {
                effectSource.RemoveWhere(e => e is TriggerSource);
                if (effectSource.Count == 0)
                {
                    _effectSources.Remove(effect.Key);
                }
                else
                {
                    removedEffects.Remove(effect);
                }
            }
        }
        
        if (removedEffects.Count == 0) return;
        
        ActiveEffects = ActiveEffects.RemoveRange(removedEffects);
        
        _updater.OnUnitUpdate(this, new UnitUpdate
        {
            Buffs = _buffs,
            Effects = ActiveEffects.ToInfoDictionary()
        });
    }

    private void RemoveExpiredEffects()
    {
        var removedEffects = ActiveEffects.Where(effect => effect.IsExpired).ToList();
        foreach (var effect in removedEffects)
        {
            _effectSources.Remove(effect.Key);
        }
        
        ActiveEffects = ActiveEffects.RemoveRange(removedEffects);
        _updater.OnUnitUpdate(this, new UnitUpdate
        {
            Buffs = _buffs,
            Effects = ActiveEffects.ToInfoDictionary()
        });
    }

    public void PurgeEffects(bool positive, bool negative)
    {
        if (!positive && !negative) return;
        
        var purgedEffects = ActiveEffects.Where(effect =>
        {
            var eCard = effect.Card;
            var remove = false;
            if (negative)
            {
                remove = effect.TimestampEnd.HasValue && !eCard.Positive;
            }

            if (!remove && positive)
            {
                remove = effect.TimestampEnd.HasValue && eCard.Positive;
            }
            
            return remove;
        }).ToList();
        
        foreach (var effect in purgedEffects)
        {
            _effectSources.Remove(effect.Key);
        }
        
        ActiveEffects = ActiveEffects.RemoveRange(purgedEffects);
    }

    private void CreateIntervalUpdater(Key constKey, ConstEffect effect)
    {
        switch (effect)
        {
            case ConstEffectAura { Interval: > 0, IntervalEffects: not null } aura:
                RunInterval(aura.Interval, aura.IntervalEffects, 
                    () => ActiveEffects.Exists(e => e.Key == constKey),
                    () =>
                    {
                        UnitsInAuraSinceLastUpdate.TryGetValue(aura, out var units);
                        return units ?? [];
                    }, 
                    (units, instEffects) =>
                    {
                        var impact = CreateImpactData();
                        _updater.OnApplyInstEffect(GetSelfSource(impact), units, instEffects, impact);
                    });
                break;
            case ConstEffectInterval { Interval: > 0, IntervalEffects: not null } interval:
                RunInterval(interval.Interval, interval.IntervalEffects,
                    () => ActiveEffects.Exists(e => e.Key == constKey), 
                    () => [this], 
                    (units, instEffects) =>
                    {
                        var source = _effectSources.GetValueOrDefault(constKey)?.First() ?? GetSelfSource(CreateImpactData());
                        _updater.OnApplyInstEffect(source, units, instEffects, source.Impact ?? CreateImpactData());
                    });
                break;
            case ConstEffectSelf { Interval: > 0, IntervalEffects: not null } self:
                RunInterval(self.Interval, self.IntervalEffects, 
                    () => ActiveEffects.Exists(e => e.Key == constKey),
                    () => [this], 
                    (units, instEffects) =>
                    {
                        var impact = CreateImpactData();
                        _updater.OnApplyInstEffect(GetSelfSource(impact), units, instEffects, impact);
                    });
                break;
        }
    }

    public bool IsHealth => UnitCard?.Health != null;

    public bool IsForcefield => UnitCard?.Health is { Forcefield: not null };

    public float HealthPercentage => UnitCard?.Health?.Health != null ? _health / this.UnitMaxHealth(UnitCard.Health.Health.MaxHealth) : 1;

    public bool IsLowHealth => UnitCard?.Health?.Health != null &&
                               _health / this.UnitMaxHealth(UnitCard.Health.Health.MaxHealth) <=
                               CatalogueHelper.GlobalLogic.GuiLogic?.LowHealthVignettePercent;

    public bool IsLowAmmo => Gears.Any(g => g.Ammo.Any(a =>
        a.Pool / a.PoolSize <= CatalogueHelper.GlobalLogic.GuiLogic?.LowHealthVignettePercent));

    public bool IsInsideUnit(Vector3s blockPos) => UnitSizeHelper.IsInsideUnit(blockPos, this);

    public CardSkin? SkinCard => Databases.Catalogue.GetCard<CardSkin>(SkinKey);

    public CardAbility? AbilityCard =>
        AbilityKey.HasValue ? Databases.Catalogue.GetCard<CardAbility>(AbilityKey.Value) : null;

    public void StartRecall() => IsRecall = true;

    public void EndRecall() => IsRecall = false;

    public Key CurrentGearKey
    {
        get
        {
            var gearByIndex = GetGearByIndex(_currentGearIndex);
            return gearByIndex?.Key ?? Key.None;
        }
    }

    public GearData? CurrentGear => GetGearByIndex(_currentGearIndex);

    public GearData? GetGearByIndex(int index) => index >= 0 && index < Gears.Count ? Gears[index] : null;

    public GearData? GetGearByKey(Key gearKey) => Gears.Find((Predicate<GearData>)(i => i.Key == gearKey));

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
        var impact = CreateImpactData();
        foreach (var switchEffect in switchEffects.Select(sw => sw.Effect).OfType<InstEffect>())
        {
            _updater.OnApplyInstEffect(GetSelfSource(impact), [this], switchEffect, impact);
        }
    }

    public void UpdateMapBlocks(HashSet<Vector3s> blocks) => OverlappingMapBlocks = blocks;

    public bool ShowerStarted;
    public void StartShower(Action<Random> showerAction)
    {
        if (ShowerStarted || UnitCard?.Data is not UnitDataShower shower) return;
        ShowerStarted = true;
        RunShower(shower, showerAction);
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
    
    public UnitDataPiggyBank? PiggyBankData => UnitCard?.Data as UnitDataPiggyBank;

    public ulong LastMoveUpdateTime;
    private ulong _lastUpdateTime;
    
    private readonly UnitUpdater _updater;
    
    private bool IsNewUpdate(ulong updateTime)
    {
        if (_lastUpdateTime >= updateTime) return false;
        _lastUpdateTime = updateTime;
        return true;
    }
    
    private bool IsNewMoveUpdate(ulong updateTime) => LastMoveUpdateTime < updateTime;

    public Unit(uint id, UnitInit unitInit, UnitUpdater updater)
    { 
        Id = id;
        Key = unitInit.Key;
        if (unitInit.Transform != null) 
            Transform = unitInit.Transform;
        Controlled = unitInit.Controlled;
        OwnerPlayerId = unitInit.OwnerId;
        PermaOwnerPlayerId = unitInit.OwnerId;
        Team = unitInit.Team;
        PermaTeam = unitInit.Team;
        PlayerId = unitInit.PlayerId;
        if (unitInit.SkinKey.HasValue)
            SkinKey = unitInit.SkinKey.Value;
        if (unitInit.Gears != null)
            Gears = unitInit.Gears.Select((key, index) => new GearData(this, key, index)).ToList();

        _constEffects.CollectionChanged += OnEffectsChanged;

        SelfSource = new UnitSource(this);

        CreationTime = DateTimeOffset.Now;
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

            if (UnitCard.Lifetime is not null)
            {
                _expirationTime = DateTimeOffset.Now.AddSeconds(UnitCard.Lifetime.Value);
            }
            
            switch (uCard.Data)
            {
                case UnitDataCloud unitDataCloud:
                    CloudEffect = new EffectArea(new BoundingSphere(GetMidpoint(), unitDataCloud.Range)); 
                    break;
                
                case UnitDataDamageCapture unitDataDamageCapture:
                    var midPoint = GetMidpoint();
                    var captureMidpoint = midPoint with { Y = midPoint.Y + unitDataDamageCapture.CaptureZone.Y / 2 };
                    DamageCaptureEffect = new EffectArea(new BoundingBoxEx(captureMidpoint, unitDataDamageCapture.CaptureZone));
                    break;
                
                case UnitDataLandmine unitDataLandmine:
                    if (unitDataLandmine.Timeout is not null)
                        _expirationTime = DateTimeOffset.Now.AddSeconds(unitDataLandmine.Timeout.Value);
                    LandmineEffect = new EffectArea(new BoundingSphere(GetMidpoint(), unitDataLandmine.TriggerRadius));
                    break;
                
                case UnitDataPickup { Timeout: not null } unitDataPickup:
                    _expirationTime = DateTimeOffset.Now.AddSeconds(unitDataPickup.Timeout.Value);
                    break;
                
                case UnitDataTeslaCoil unitDataTeslaCoil:
                    _charges = unitDataTeslaCoil.InitCharges;
                    break;
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

    public void UpdateData(UnitUpdate data, ulong? updateTime = null, bool unbuffered = false)
    {
        if (updateTime.HasValue && !IsNewUpdate(updateTime.Value)) return;
        
        var effectsBeforeUpdate = ActiveEffects;
        var buffsBeforeUpdate = _buffs;
        
        if (data.Team.HasValue)
            Team = data.Team.Value;
        
        if (data.Buffs != null)
        {
            _buffs = data.Buffs;
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
            var oldHealth = _health;
            _health = data.Health.Value;

            if (UnitCard?.Health?.Health is not null && !IsDead)
            {
                var maxHp = this.UnitMaxHealth(UnitCard.Health.Health.MaxHealth);
                if (maxHp > 0)
                {
                    var oldHealthPercentage = oldHealth / maxHp;
                    var currHealthPercentage = _health / maxHp;
                    var impact = CreateImpactData();
                    var source = GetSelfSource(impact);
                    foreach (var effect in ActiveEffects.GetEffectsOfType<ConstEffectOnLowHealth>())
                    {
                        if (currHealthPercentage <= effect.HealthThreshold && oldHealthPercentage > effect.HealthThreshold)
                        {
                            if (effect.OnThresholdDown is { } thresholdDown)
                            {
                                _updater.OnApplyInstEffect(source, [this], thresholdDown, impact);
                            }

                            if (effect.ConstantEffects is { } constantEffects)
                            {
                                AddEffects(constantEffects.Select(k => new ConstEffectInfo(k)), Team, source);
                            }
                        }
                        else if (currHealthPercentage > effect.HealthThreshold &&
                                 oldHealthPercentage <= effect.HealthThreshold && 
                                 effect.ConstantEffects is { } constantEffects)
                        {
                            RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)), Team, SelfSource);
                        }
                    }

                    if (currHealthPercentage >= 0.9999f)
                    {
                        TimeAtMaxHp.Start();
                    }
                    else
                    {
                        TimeAtMaxHp.Stop();
                    }

                    if (IsLowHealth)
                    {
                        TimeAtLowHp.Start();
                    }
                    else
                    {
                        TimeAtLowHp.Stop();
                    }
                }
            }
        }
            
        if (data.CurrentGear.HasValue)
            _currentGearIndex = GearKeyToIndex(data.CurrentGear.Value);
        if (data.CapturePoints.HasValue)
            _capturePoints = data.CapturePoints.Value;
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
            _forcefield = data.Forcefield.Value;
        if (data.Shield.HasValue)
            _shield = data.Shield.Value;
        if (data.AbilityCharges.HasValue)
            AbilityCharges = data.AbilityCharges.Value;
        if (data.AbilityChargeCooldownEnd.HasValue)
            _abilityChargeCooldownEnd = (long)data.AbilityChargeCooldownEnd.Value;
        if (data.Ability.HasValue)
            AbilityKey = data.Ability.Value;
        if (data.ProjectileInitSpeed.HasValue)
            _unitProjectileInitSpeed = data.ProjectileInitSpeed.Value;
        if (data.BombTimeoutEnd.HasValue)
            _bombTimeoutEnd = data.BombTimeoutEnd.Value != 0UL
                ? DateTimeOffset.FromUnixTimeMilliseconds((long)data.BombTimeoutEnd.Value)
                : null;
        if (data.CloudAffectedBlocks != null)
            CloudAffectedBlocks = data.CloudAffectedBlocks;
        if (data.MovementActive.HasValue)
            _isCommonMovementActive = data.MovementActive.Value;
        if (data.DamageCapturers != null)
            _damageCapturers = data.DamageCapturers;
        if (data.PortalLink != null)
            PortalLinked = data.PortalLink;
        if (data.TeslaCharge.HasValue)
            TeslaCharge = data.TeslaCharge.Value;

        if (!effectsBeforeUpdate.SequenceEqual(ActiveEffects))
        {
            data.Effects = ActiveEffects.ToInfoDictionary();
        }

        if (buffsBeforeUpdate != _buffs)
        {
            data.Buffs ??= _buffs;
        }
        
        _skipBuffSet = false;
            
        _updater.OnUnitUpdate(this, data, unbuffered);
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
                var ammo = gear.Value.Ammo
                    .Select(ammo => new Ammo { Index = ammo.AmmoIndex, Mag = ammo.Mag, Pool = ammo.Pool }).ToList();
                newUpdate.Ammo.Add(gear.Key, ammo);
            }
            
            newUpdate.CurrentGear = GetGearByIndex(_currentGearIndex)?.Key;
        }

        if (UnitCard?.Health?.Health != null)
        {
            newUpdate.Health = _health;
            newUpdate.Shield = _shield;
        }

        if (UnitCard?.Health?.Forcefield != null)
        {
            newUpdate.Forcefield = _forcefield;
        }

        if (_capturePoints > 0)
        {
            newUpdate.CapturePoints = _capturePoints;
        }
        
        newUpdate.Team = Team;
        
        if (Resource > 0)
        {
            newUpdate.Resource = Resource;
        }
        
        newUpdate.Buffs = _buffs;
        newUpdate.Effects = ActiveEffects.ToInfoDictionary();

        if (UnitCard?.Data is UnitDataPlayer)
        {
            newUpdate.Devices = Devices;
            newUpdate.Ability = AbilityKey;
            newUpdate.AbilityCharges = AbilityCharges;
            newUpdate.AbilityChargeCooldownEnd = (ulong) _abilityChargeCooldownEnd;
            newUpdate.MovementActive = _isCommonMovementActive;
        }

        if (UnitCard?.Data is UnitDataTurret)
        {
            newUpdate.TurretTargetId = TurretTargetId;
        }

        if (UnitCard?.Data is UnitDataProjectile)
        {
            newUpdate.ProjectileInitSpeed = _unitProjectileInitSpeed;
        }

        if (UnitCard?.Data is UnitDataBomb)
        {
            newUpdate.BombTimeoutEnd = (ulong?)_bombTimeoutEnd?.ToUnixTimeMilliseconds();
        }

        if (UnitCard?.Data is UnitDataPortal)
        {
            newUpdate.PortalLink = PortalLinked;
        }

        if (UnitCard?.Data is UnitDataTeslaCoil)
        {
            newUpdate.TeslaCharge = TeslaCharge;
        }

        if (UnitCard?.Data is UnitDataDamageCapture)
        {
            newUpdate.DamageCapturers = _damageCapturers;
        }
        
        return newUpdate;
    }

    public bool UnitMove(ZoneTransform transform, ulong moveTime)
    {
        if(!IsNewMoveUpdate(moveTime) || IsDead) return true;

        var wasSprinting = Transform.IsSprint;
        
        var oldPosition = Transform.Position;
        LastMoveUpdateTime = moveTime;
        Transform = transform; 
        foreach (var aura in AuraEffects)
        {
            AuraEffects[aura.Key] = aura.Value.GetShapeAtNewPosition(GetMidpoint());
        }

        foreach (var nearby in NearbyBlockEffects)
        {
            NearbyBlockEffects[nearby.Key] = nearby.Value.GetShapeAtNewPosition(GetMidpoint());
        }

        CloudEffect?.AreaMoved(GetMidpoint(transform.Position));
        DamageCaptureEffect?.AreaMoved(GetMidpoint(transform.Position));
        LandmineEffect?.AreaMoved(GetMidpoint(transform.Position));

        if (!wasSprinting && transform.IsSprint)
        {
            var impact = CreateImpactData();
            foreach (var sprintEffect in ActiveEffects.GetEffectsOfType<ConstEffectOnSprint>())
            {
                if (sprintEffect.ConstantEffects is { } constantEffects)
                {
                    AddEffects(constantEffects.Select(k => new ConstEffectInfo(k)), Team, GetSelfSource(impact));
                }
            }
        }
        else if (wasSprinting && !transform.IsSprint)
        {
            foreach (var sprintEffect in ActiveEffects.GetEffectsOfType<ConstEffectOnSprint>())
            {
                if (sprintEffect.ConstantEffects is { } constantEffects)
                {
                    RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)), Team, SelfSource);
                }
            }
        }

        if (PlayerId is not null)
        {
            MoveStatsUpdate(transform, oldPosition);
        }
        
        _updater.OnUnitMove(this, moveTime, transform, oldPosition);
        return true;
    }

    public void ReloadAmmo(bool allGear, float percentage)
    {
        if (allGear)
        {
            var ammoDict = new Dictionary<Key, List<Ammo>>();
            foreach (var gear in Gears)
            {
                var updatedAmmo = new List<Ammo>();
                foreach (var tool in gear.Tools.Where(tool => tool.IsPossibleToReload()))
                {
                    if (tool.GetAmmoData() is not { } ammoData) continue;
                    var newPartialAmmo = ammoData.ReloadPartialAmmo(percentage * ammoData.MagSize);
                    if (newPartialAmmo is not null) 
                        updatedAmmo.Add(newPartialAmmo);
                }

                if (updatedAmmo.Count > 0)
                {
                    ammoDict.Add(gear.Key, updatedAmmo);
                }
            }

            if (ammoDict.Count > 0)
            {
                UpdateData(new UnitUpdate { Ammo = ammoDict }, null, true);
            }
        }
        else
        {
            if (CurrentGear?.Tools is null) return;
            var updatedAmmo = new List<Ammo>();
            foreach (var tool in CurrentGear.Tools.Where(tool => tool.IsPossibleToReload()))
            {
                if (tool.GetAmmoData() is not { } ammoData) continue;
                var newPartialAmmo = ammoData.ReloadPartialAmmo(percentage * ammoData.MagSize);
                if (newPartialAmmo is not null)
                    updatedAmmo.Add(newPartialAmmo);
            }
            
            if (updatedAmmo.Count > 0)
            {
                UpdateData(new UnitUpdate
                {
                    Ammo = new Dictionary<Key, List<Ammo>> { {CurrentGear.Key, updatedAmmo} }
                }, null, true);
            }
        }
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
            }, null, true);
        }
    }

    public void CleanUpExpired()
    {
        RemoveExpiredEffects();
        foreach (var assister in RecentDamagers.Where(assister => assister.Value < DateTimeOffset.Now))
        {
            RecentDamagers.Remove(assister.Key);
        }

        if (_rechargeForcefieldTime is not null && UnitCard?.Health?.Forcefield is { } forcefield &&
            DateTimeOffset.Now > _rechargeForcefieldTime)
        {
            AddForcefield(forcefield.RechargeRate);
            if (this.UnitMaxForcefield(forcefield.MaxAmount) <= _forcefield)
            {
                _rechargeForcefieldTime = null;
            }
            else
            {
                _rechargeForcefieldTime = DateTimeOffset.Now.AddSeconds(forcefield.HitRechargeDelay);
            }
        }

        if (IsExpired || IsFuseExpired)
        {
            var impact = CreateBlankImpactData();
            Killed(impact);
        }

        if (SpawnProtectionTime is not null && !HasSpawnProtection)
        {
            SpawnProtectionTime = null;
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
        var removed = removedItems.Select(info => (info.Key.GetCard<CardEffect>(), info)).ToList();
        var added = addedItems.Select(info => (info.Key.GetCard<CardEffect>(), info)).ToList();
        if (added.Count == 0 && removed.Count == 0) return;
        
        if (!_skipBuffSet && removed.Exists(eff => eff.Item1?.Effect is ConstEffectBuff) ||
                              added.Exists(eff => eff.Item1?.Effect is ConstEffectBuff))
        {
            _buffs = ExtractBuffs(e.NewItem.Select(info => info.Key).Distinct());
            if (UnitCard?.IsObjective is true && !_updater.DoesObjBuffApply(Team, UnitCard?.Labels ?? []))
            {
                ActiveEffects = ActiveEffects.RemoveAll(e => CatalogueHelper.ObjectiveShieldKeys.Contains(e.Key));
            }

            if (IsBuff(BuffType.Disabled) && !_wasDisabled)
            {
                OnDisabled();
            }
            else if (_wasDisabled && !IsBuff(BuffType.Disabled))
            {
                OnReEnabled();
            }

            if (IsBuff(BuffType.Disarm) && !_wasDisarmed)
            {
                _wasDisarmed = true;
                _updater.OnDisarmed(this);
            }
            else if (!IsBuff(BuffType.Disarm) && _wasDisarmed)
            {
                _wasDisarmed = false;
            }

            if (PlayerId is not null)
            {
                if (IsBuff(BuffType.VisionMark))
                {
                    TimeSpotted.Start();
                }
                else
                {
                    TimeSpotted.Stop();
                }

                if (IsBuff(BuffType.Confusion) || IsBuff(BuffType.Disarm) || IsBuff(BuffType.Disabled) ||
                    IsBuff(BuffType.Sway) || IsBuff(BuffType.Root))
                {
                    TimeControlled.Start();
                }
                else
                {
                    TimeControlled.Stop();
                }
            }
            
            switch (_wasConfused)
            {
                case false when IsBuff(BuffType.Confusion):
                    var confuser = ActiveEffects.FindAll(e =>
                    {
                        var eCard = e.Card;
                        return eCard.Effect is ConstEffectBuff { Buffs: not null } eff &&
                               eff.Buffs.ContainsKey(BuffType.Confusion);
                    }).Select(e => _effectSources.GetValueOrDefault(e.Key)?
                        .First(s => s is UnitSource
                        {
                            Unit.OwnerPlayerId: not null
                        })).OfType<UnitSource>().FirstOrDefault();

                    if (confuser is not null)
                    {
                        OnConfused(confuser.Unit);
                    }
                    break;
                
                case true when IsBuff(BuffType.Confusion):
                    var confuseEffects = ActiveEffects.FindAll(e =>
                    {
                        var eCard = e.Card;
                        return eCard.Effect is ConstEffectBuff { Buffs: not null } eff &&
                               eff.Buffs.ContainsKey(BuffType.Confusion);
                    });

                    if (confuseEffects.Select(e => _effectSources.GetValueOrDefault(e.Key)?
                            .All(s => s is UnitSource unitSource && unitSource.Unit.OwnerPlayerId == OwnerPlayerId))
                        .Any(b => b is true))
                    {
                        var newConfuser = confuseEffects.Select(e => _effectSources.GetValueOrDefault(e.Key)?
                            .First(s => s is UnitSource
                            {
                                Unit.OwnerPlayerId: not null
                            })).OfType<UnitSource>().FirstOrDefault();

                        if (newConfuser is not null)
                        {
                            OnConfused(newConfuser.Unit);
                        }
                        else
                        {
                            OnUnconfused();
                        }
                    }
                    break;
                
                case true when !IsBuff(BuffType.Confusion):
                    OnUnconfused();
                    break;
            }
        }
        
        var center = GetMidpoint();
        foreach (var (effect, info) in removed)
        {
            switch (effect?.Effect)
            {
                case ConstEffectAura aura:
                    AuraEffects.Remove(aura);
                    UnitsInAuraSinceLastUpdate.Remove(aura, out var affectedUnits);
                    if (affectedUnits != null)
                    {
                        var constEffects = aura.ConstantEffects?.Select(k => new ConstEffectInfo(k, null)).ToList() ??
                                           [];
                        foreach (var affectedUnit in affectedUnits)
                        {
                            affectedUnit.RemoveEffects(constEffects, Team, SelfSource);
                        }
                    }
                    break;
                case ConstEffectOnMatchContext matchContext:
                    MatchContextEffects.Remove(matchContext);
                    break;
                case ConstEffectOnNearbyBlock nearby:
                    NearbyBlockEffects.Remove(nearby);
                    if (nearby.Effects is { Count: > 0 } blockEffects)
                        RemoveEffects(blockEffects.Select(k => new ConstEffectInfo(k, null)), Team, null,
                            true);
                    break;
                case ConstEffectPull:
                    var pullers = _effectSources.GetValueOrDefault(info.Key);
                    if (pullers is null || pullers.Select(p => p as UnitSource).OfType<UnitSource>()
                            .Select(p => p.Unit.Id).All(i => i != _activePuller))
                    {
                        _activePuller = null;
                        _updater.OnPull(this, new ManeuverPull
                        {
                            Enabled = false
                        });
                    }
                    break;
                case ConstEffectSelf constEffectSelf:
                    if (constEffectSelf.ConstantEffects is { Count: > 0 } effects)
                    {
                        RemoveEffects(effects.Select(k => new ConstEffectInfo(k, null)), Team, SelfSource);
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
                case ConstEffectAura { InnerRadius: not null } aura
                    when !(Math.Abs(aura.InnerRadius.Value - aura.OuterRadius) < 0.0001):
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
                case ConstEffectOnMatchContext matchContext:
                    MatchContextEffects.Add(matchContext);
                    break;
                case ConstEffectOnNearbyBlock nearby:
                    NearbyBlockEffects.Add(nearby, new BoundingSphere(center, nearby.Radius));
                    break;
                case ConstEffectPull pullEffect:
                    var pullers = _effectSources.GetValueOrDefault(info.Key);
                    if (pullers is null) 
                        break;

                    foreach (var puller in pullers)
                    {
                        var src = puller switch
                        {
                            UnitSource unitSource1 => unitSource1.Unit,
                            _ => null
                        };
                        if (src is null) continue;
                        if (DoPull(src, pullEffect))
                        {
                            _updater.OnPull(this, new ManeuverPull
                            {
                                OriginPos = src.Transform.Position,
                                OriginUnitId = src.Id,
                                Force = pullEffect.Force,
                                Enabled = true
                            });
                        }
                    }

                    break;
                case ConstEffectSelf constEffectSelf:
                    CreateIntervalUpdater(info.Key, constEffectSelf);
                    if (constEffectSelf.ConstantEffects is { Count: > 0 } effects)
                        AddEffects(effects.Select(k => new ConstEffectInfo(k)), Team, SelfSource);
                    break;
                case ConstEffectTeam:
                    _updater.OnTeamEffectAdded(this, info);
                    break;
            }
        }
    }
}