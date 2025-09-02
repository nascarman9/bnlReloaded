using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class Unit
{
    public uint Id;
    public ulong LastUpdateTime;
    public TeamType Team;
    public ZoneTransform Transform = new();
    public bool Controlled;
    public float Shield;
    public bool IsDeath;
    public List<ConstEffectInfo> ConstEffects = [];
    public List<ConstEffectInfo> ActualConstEffects = [];
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
    private bool isAbilityAvailableCache;
    private int isAbilityAvailableCacheFrame;
    public bool IsRecall;
    public float RecallDuration;
    public ulong RecallEnd;
    public List<GearData> Gears = [];
    public int CurrentGearIndex = -1;
    public Dictionary<int, DeviceData> Devices = new();
    private int? currentDeviceSlot;
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
    public List<string> CurrentEffectsDebug = [];

    public CardUnit UnitCard => Databases.Catalogue.GetCard<CardUnit>(Key)!;

    public List<ConstEffectInfo> GetActualConstEffects()
    {
        var actualConstEffects = new List<ConstEffectInfo>();
        foreach (var constEffect in ConstEffects)
        {
            var c = constEffect;
            var constEffectInfo = actualConstEffects.Find((Predicate<ConstEffectInfo>)(r => r.Key == c.Key));
            if (constEffectInfo == null)
                actualConstEffects.Add(c);
            else if (constEffectInfo.TimestampEnd.HasValue && c.TimestampEnd.HasValue &&
                     constEffectInfo.TimestampEnd.Value < c.TimestampEnd.Value)
            {
                actualConstEffects.Remove(constEffectInfo);
                actualConstEffects.Add(c);
            }
        }

        return actualConstEffects;
    }

    public bool IsBuff(BuffType buff) => Buffs.ContainsKey(buff);

    public float GetBuff(BuffType buff, float def = 0)
    {
        return Buffs.GetValueOrDefault(buff, def);
    }

    public bool IsHealth => UnitCard.Health != null;

    public bool IsForcefield => UnitCard.Health is { Forcefield: not null };

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

    public UnitDataPlayer? PlayerUnitData => UnitCard.Data as UnitDataPlayer;

    public UnitDataTurret? TurretUnitData => UnitCard.Data as UnitDataTurret;

    public UnitDataMortar? MortarUnitData => UnitCard.Data as UnitDataMortar;

    public UnitDataLandmine? LandmineUnitData => UnitCard.Data as UnitDataLandmine;

    public UnitDataBomb? BombUnitData => UnitCard.Data as UnitDataBomb;

    public UnitDataPickup? PickupUnitData => UnitCard.Data as UnitDataPickup;

    public UnitDataProjectile? ProjectileUnitData => UnitCard.Data as UnitDataProjectile;

    public UnitDataCloud? CloudUnitData => UnitCard.Data as UnitDataCloud;

    public UnitDataSkybeam? SkybeamUnitData => UnitCard.Data as UnitDataSkybeam;

    public UnitDataTeslaCoil? TeslaUnitData => UnitCard.Data as UnitDataTeslaCoil;

    public UnitDataShower? ShowerUnitData => UnitCard.Data as UnitDataShower;

    public UnitDataDrill? DrillUnitData => UnitCard.Data as UnitDataDrill;

    public bool IsNewUpdate(ulong updateTime)
    {
        if (LastUpdateTime >= updateTime) return false;
        LastUpdateTime = updateTime;
        return true;
    }

    public void InitData(uint id, UnitInit unitInit)
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

    public void UpdateData(UnitUpdate data)
    {
        if (data.Ammo != null)
        {
            foreach (var keyValuePair in data.Ammo)
            {
                var gearByKey = GetGearByKey(keyValuePair.Key);
                gearByKey?.ServerUpdateAmmo(keyValuePair.Value);
            }
        }

        if (data.Health.HasValue)
            Health = data.Health.Value;
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
        if (data.Buffs != null)
            Buffs = data.Buffs;
        if (data.Effects != null)
        {
            ConstEffects = ConstEffectInfo.Convert(data.Effects);
            CurrentEffectsDebug = ConstEffects.ConvertAll((Converter<ConstEffectInfo, string>)(i => i.Card.Id));
            ActualConstEffects = GetActualConstEffects();
        }

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
        if (!data.TeslaCharge.HasValue)
            return;
        TeslaCharge = data.TeslaCharge.Value;
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

        if (UnitCard.Health?.Health != null)
        {
            newUpdate.Health = Health;
            newUpdate.Shield = Shield;
        }

        if (UnitCard.Health?.Forcefield != null)
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
        newUpdate.Effects = ConstEffects.ToDictionary(effect => effect.Key, effect => effect.TimestampEnd);

        if (UnitCard.Data is UnitDataPlayer)
        {
            newUpdate.Devices = Devices;
            newUpdate.Ability = AbilityKey;
            newUpdate.AbilityCharges = AbilityCharges;
            newUpdate.AbilityChargeCooldownEnd = (ulong) AbilityChargeCooldownEnd;
            newUpdate.MovementActive = IsCommonMovementActive;
        }

        if (UnitCard.Data is UnitDataTurret)
        {
            newUpdate.TurretTargetId = TurretTargetId;
        }

        if (UnitCard.Data is UnitDataProjectile)
        {
            newUpdate.ProjectileInitSpeed = UnitProjectileInitSpeed;
        }

        if (UnitCard.Data is UnitDataBomb)
        {
            newUpdate.BombTimeoutEnd = BombTimoutEnd;
        }

        if (UnitCard.Data is UnitDataCloud)
        {
            newUpdate.CloudAffectedBlocks = CloudAffectedBlocks;
        }

        if (UnitCard.Data is UnitDataPortal)
        {
            newUpdate.PortalLink = PortalLinked;
        }

        if (UnitCard.Data is UnitDataTeslaCoil)
        {
            newUpdate.TeslaCharge = TeslaCharge;
        }

        if (DamageCapturers.Count > 0)
        {
            newUpdate.DamageCapturers = DamageCapturers;
        }
        
        return newUpdate;
    }
}