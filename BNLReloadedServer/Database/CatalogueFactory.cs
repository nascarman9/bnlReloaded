using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ServerTypes;

namespace BNLReloadedServer.Database;

public static class CatalogueFactory
{
    private static ulong _nextGameId;
    
    public static CustomGameInfo CreateCustomGame(string name, string password, string playerName)
    {
        var customLogic = CatalogueHelper.GlobalLogic.CustomGame;
        var maps = CatalogueHelper.MapList.Custom;
        return new CustomGameInfo
        {
            Id = Interlocked.Increment(ref _nextGameId),
            GameName = name,
            StarterNickname = playerName,
            Players = 0,
            MaxPlayers = 10,
            Private = password != string.Empty,
            MapInfo = new MapInfoCard
            {
               MapKey = maps?[0] ?? Key.None
            },
            BuildTime = customLogic?.DefaultBuildTime ?? 300f,
            RespawnTimeMod = 0,
            HeroSwitch = false,
            SuperSupply = false,
            AllowBackfilling = true,
            ResourceCap = customLogic?.DefaultResourceCap ?? 7500f,
            InitResource = customLogic?.DefaultInitResource ?? 4000f,
            Status = CustomGameStatus.Preparing,
        };
    }
    
    public static Unit? CreateUnit(uint id, MapUnit unit, UnitUpdater updater)
    {
        var unitCard = Databases.Catalogue.GetCard<CardUnit>(unit.UnitKey);
        if (unitCard == null) return null;
        
        var initTransform = new ZoneTransform
        {
            Position = unit.Position,
            Rotation = unit.Rotation,
            LocalVelocity = Vector3s.Zero
        };
        
        var unitInit = new UnitInit
        {
            Key = unit.UnitKey,
            Transform = initTransform,
            Controlled = true,
            Team = unit.Team,
        };

        var newUnit = new Unit(id, unitInit, updater);

        var startingEffects = newUnit.InitialEffects.ToDictionary();
        foreach (var effect in updater.GetTeamEffects(unit.Team).Where(e => newUnit.DoesEffectApply(e, unit.Team)))
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
        newUnit.ActiveEffects = ConstEffectInfo.Convert(startingEffects);
        
        var initUpdate = new UnitUpdate
        {
            Team = unit.Team,
            Health = unitCard.Health?.Health != null
                ? newUnit.UnitMaxHealth(unitCard.Health.Health.MaxHealth)
                : null,
            Forcefield = unitCard.Health?.Forcefield != null
                ? newUnit.UnitMaxForcefield(unitCard.Health.Forcefield.MaxAmount)
                : null,
            Shield = unitCard.Health?.Health?.Shield,
            Effects = newUnit.ActiveEffects.ToInfoDictionary(),
            MovementActive = unitCard.Movement is null or UnitMovementStatic ? null : false,
            BombTimeoutEnd = unitCard.Data is UnitDataBomb bombData
                ? (ulong)DateTimeOffset.Now.AddSeconds(bombData.Timeout).ToUnixTimeMilliseconds()
                : null,
            TeslaCharge = unitCard.Data is UnitDataTeslaCoil teslaData 
                ? teslaData.InitCharges > 0 
                    ? teslaData.InitCharges == teslaData.MaxCharges 
                        ? TeslaChargeType.FullSelfCharge 
                        : TeslaChargeType.SelfCharge
                    : TeslaChargeType.NoCharge
                : null,
            DamageCapturers = unitCard.Data is UnitDataDamageCapture
                ? []
                : null
        };
        
        newUnit.UpdateData(initUpdate);
        
        return newUnit;
    }

    public static Unit? CreateUnit(uint id, Key unitKey, ZoneTransform location, TeamType team, Unit? owner,
        UnitUpdater updater, float speed = 0, bool isAttached = false)
    {
        var unit = Databases.Catalogue.GetCard<CardUnit>(unitKey);
        if (unit == null) return null;
        
        var unitInit = new UnitInit
        {
            Key = unit.Key,
            Transform = location,
            Controlled = owner?.OwnerPlayerId == null,
            OwnerId = owner?.OwnerPlayerId,
            Team = team,
        };

        var newUnit = new Unit(id, unitInit, updater);
        
        var startingEffects = newUnit.InitialEffects.ToDictionary();
        foreach (var effect in updater.GetTeamEffects(team).Where(e => newUnit.DoesEffectApply(e, team)))
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
        newUnit.ActiveEffects = ConstEffectInfo.Convert(startingEffects);
        
        var initUpdate = new UnitUpdate
        {
            Team = team,
            Health = unit.Health?.Health != null
                ? newUnit.UnitMaxHealth(unit.Health.Health.MaxHealth)
                : null,
            Forcefield = unit.Health?.Forcefield != null
                ? newUnit.UnitMaxForcefield(unit.Health.Forcefield.MaxAmount)
                : null,
            Shield = unit.Health?.Health?.Shield,
            Effects = newUnit.ActiveEffects.ToInfoDictionary(),
            MovementActive = unit.Movement is null or UnitMovementStatic ? null : !isAttached,
            BombTimeoutEnd = unit.Data is UnitDataBomb bombData
                ? (ulong)DateTimeOffset.Now.AddSeconds(bombData.Timeout).ToUnixTimeMilliseconds()
                : null,
            TeslaCharge = unit.Data is UnitDataTeslaCoil teslaData 
                ? teslaData.InitCharges > 0 
                    ? teslaData.InitCharges == teslaData.MaxCharges 
                        ? TeslaChargeType.FullSelfCharge 
                        : TeslaChargeType.SelfCharge
                    : TeslaChargeType.NoCharge
                : null,
            ProjectileInitSpeed = unit.Data is UnitDataProjectile ? speed : null
        };
        
        newUnit.UpdateData(initUpdate);
        
        return newUnit;
    }

    public static Unit? CreatePlayerUnit(uint id, uint playerId, ZoneTransform transform, PlayerLobbyState playerInfo,
        IGameInitiator gameInitiator, CardMatch matchCard, UnitUpdater updater)
    {
        var unitCard = Databases.Catalogue.GetCard<CardUnit>(playerInfo.Hero);
        if (unitCard is not { Data: UnitDataPlayer playerData }) return null;
        
        var unitInit = new UnitInit
        {
            Key = playerInfo.Hero,
            Transform = transform,
            Controlled = false,
            OwnerId = playerId,
            Team = playerInfo.Team,
            PlayerId = playerId,
            SkinKey = playerInfo.SkinKey,
            Gears = playerData.Gears?.ConvertGear(playerInfo.Perks ?? []),
        };

        var newUnit = new Unit(id, unitInit, updater);
        
        var effects = PerkHelper.ExtractEffects(playerInfo.Perks ?? []);
        var passives = playerData.Passive?.ConvertPassives(playerInfo.Perks ?? []);

        if (passives is { Count: > 0 })
        {
            foreach (var effect in passives)
            {
                effects[effect] = null;
            }
        }

        if (matchCard.PlayerEffects is { Count: > 0 })
        {
            foreach (var effect in matchCard.PlayerEffects)
            {
                effects[effect] = null;
            }
        }
        
        newUnit.InitialEffects = newUnit.InitialEffects.AddRange(effects);
        
        var startingEffects = newUnit.InitialEffects.ToDictionary();
        foreach (var effect in updater.GetTeamEffects(playerInfo.Team)
                     .Where(e => newUnit.DoesEffectApply(e, playerInfo.Team)))
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
        newUnit.ActiveEffects = ConstEffectInfo.Convert(startingEffects);

        var devices = playerInfo.Devices?.ConvertDevices(playerInfo.Perks ?? []);

        var updatedDevices = new Dictionary<int, DeviceData>();

        newUnit.DeviceLevels = playerInfo.DeviceLevels ?? new Dictionary<Key, int>();

        if (devices != null)
        {
            foreach (var device in devices)
            {
                var deviceCard = Databases.Catalogue.GetCard<CardDevice>(device.Value);
                var itemKey = deviceCard?.DeviceKeyAtLevel((byte)newUnit.DeviceLevels.GetValueOrDefault(deviceCard.GroupKey, 1));
                if (!itemKey.HasValue) continue;
                var itemCard = Databases.Catalogue.GetCard(itemKey.Value);
                var deviceData = itemCard switch
                {
                    CardBlock cBlock => new DeviceData
                    {
                        DeviceKey = device.Value,
                        TotalCost = newUnit.BuildCost(cBlock.BaseCost ?? 1),
                        CostInc = 0
                    },
                    CardUnit cUnit => new DeviceData
                    {
                        DeviceKey = device.Value,
                        TotalCost = newUnit.BuildCost(cUnit.BaseCost ?? 1),
                        CostInc = 0
                    },
                    _ => null
                };

                if (deviceData == null) continue;
                if (matchCard.DevicesLogic?.DeviceCostModifiers is { Count: > 0 } costModifiers &&
                    costModifiers.TryGetValue(deviceData.DeviceKey, out var modifier))
                {
                    deviceData.TotalCost *= modifier;
                }
                updatedDevices[device.Key] = deviceData;
            }
        }

        var abilityCard =
            Databases.Catalogue.GetCard<CardAbility>(
                playerData.ActiveAbilityKey.ConvertAbility(playerInfo.Perks ?? []));
        
        foreach (var ammo in newUnit.Gears.SelectMany(gear => gear.Ammo))
        {
            ammo.Mag = ammo.MagSize;
            ammo.Pool = ammo.PoolSize;
        }
        
        Dictionary<Key, List<Ammo>> updateAmmo = [];
        foreach (var gear in newUnit.Gears)
        {
            var ammo = gear.Ammo
                .Select(ammo => new Ammo { Index = ammo.AmmoIndex, Mag = ammo.Mag, Pool = ammo.Pool }).ToList();
            updateAmmo.Add(gear.Key, ammo);
        }

        var initUpdate = new UnitUpdate
        {
            Team = playerInfo.Team,
            Health = unitCard.Health?.Health != null
                ? newUnit.UnitMaxHealth(unitCard.Health.Health.MaxHealth)
                : null,
            Forcefield = unitCard.Health?.Forcefield != null
                ? newUnit.UnitMaxForcefield(unitCard.Health.Forcefield.MaxAmount)
                : null,
            Shield = unitCard.Health?.Health?.Shield,
            Ammo = updateAmmo,
            MovementActive = false,
            CurrentGear = newUnit.Gears[0].Key,
            Ability = abilityCard?.Key,
            AbilityCharges = abilityCard?.Charges?.MaxCharges,
            AbilityChargeCooldownEnd = 0,
            Resource = gameInitiator.GetResourceAmount(),
            Effects = newUnit.ActiveEffects.ToInfoDictionary(),
            Devices = devices != null ? updatedDevices : null
        };
        
        newUnit.UpdateData(initUpdate);
        return newUnit;
    }
}