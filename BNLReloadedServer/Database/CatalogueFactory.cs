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

        newUnit.ActiveEffects = ConstEffectInfo.Convert(newUnit.InitialEffects.AddRange(updater.GetTeamEffects(unit.Team).ToList()
            .ToInfoDictionary()));
        
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
            Effects = newUnit.ActiveEffects.ToInfoDictionary()
        };
        
        newUnit.UpdateData(initUpdate);
        
        return newUnit;
    }

    public static Unit? CreateUnit(uint id, Key unitKey, ZoneTransform location, TeamType team, Unit? owner, UnitUpdater updater)
    {
        var unit = Databases.Catalogue.GetCard<CardUnit>(unitKey);
        if (unit == null) return null;
        
        var unitInit = new UnitInit
        {
            Key = unit.Key,
            Transform = location,
            Controlled = owner == null,
            OwnerId = owner?.PlayerId,
            Team = team,
        };

        var newUnit = new Unit(id, unitInit, updater);

        newUnit.ActiveEffects = ConstEffectInfo.Convert(newUnit.InitialEffects.AddRange(updater.GetTeamEffects(team).ToList().ToInfoDictionary()));
        
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
            Effects = newUnit.ActiveEffects.ToInfoDictionary()
        };
        
        newUnit.UpdateData(initUpdate);
        
        return newUnit;
    }

    public static Unit? CreatePlayerUnit(uint id, uint playerId, ZoneTransform transform, PlayerLobbyState playerInfo, IGameInitiator gameInitiator, UnitUpdater updater)
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

        if (passives != null)
        {
            foreach (var effect in passives)
            {
                effects[effect] = null;
            }
        }

        newUnit.InitialEffects = newUnit.InitialEffects.AddRange(effects);
        newUnit.ActiveEffects = ConstEffectInfo.Convert(newUnit.InitialEffects.AddRange(updater.GetTeamEffects(playerInfo.Team).ToList().ToInfoDictionary()));

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
                        CostInc = newUnit.BuildCost(cBlock.CostIncPerUnit ?? 0)
                    },
                    CardUnit cUnit => new DeviceData
                    {
                        DeviceKey = device.Value,
                        TotalCost = newUnit.BuildCost(cUnit.BaseCost ?? 1),
                        CostInc = newUnit.BuildCost(cUnit.CostIncPerUnit ?? 0)
                    },
                    _ => null
                };

                if (deviceData != null)
                {
                    updatedDevices[device.Key] = deviceData;
                }
            }
        }

        var abilityCard =
            Databases.Catalogue.GetCard<CardAbility>(
                playerData.ActiveAbilityKey.ConvertAbility(playerInfo.Perks ?? []));

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