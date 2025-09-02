using System.Runtime.CompilerServices;
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
    
    public static Unit? CreateUnit(uint id, MapUnit unit)
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

        var effects = new Dictionary<Key, ulong?>();
        if (unitCard.InitEffects != null)
        {
            foreach (var initEffect in unitCard.InitEffects) 
            {
                effects.Add(initEffect, null);            
            }
        }

        if (unitCard.EnabledEffects != null)
        {
            foreach (var enabledEffect in unitCard.EnabledEffects)
            {
                effects.Add(enabledEffect, null);
            }
        }

        var initUpdate = new UnitUpdate
        {
            Team = unit.Team,
            Health = unitCard.Health?.Health?.MaxHealth,
            Forcefield = unitCard.Health?.Forcefield?.MaxAmount,
            Shield = unitCard.Health?.Health?.Shield,
            Effects = effects
        };

        var newUnit = new Unit();
        
        newUnit.InitData(id, unitInit);
        newUnit.UpdateData(initUpdate);
        
        return newUnit;
    }

    public static Unit? CreatePlayerUnit(uint id, uint playerId, ZoneTransform transform, PlayerLobbyState playerInfo, IGameInitiator gameInitiator)
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
        
        var newUnit = new Unit();
        newUnit.InitData(id, unitInit);
        var (effects, buffs) = PerkHelper.ExtractEffectsAndBuffs(playerInfo.Perks ?? []);
        var passives = playerData.Passive?.ConvertPassives(playerInfo.Perks ?? []);
        var passiveBuffs = passives?.ExtractBuffs();
        if (passiveBuffs != null)
        {
            foreach (var buff in passiveBuffs)
            {
                if (buffs.ContainsKey(buff.Key))
                {
                    buffs[buff.Key] += buff.Value;
                }
                else
                {
                    buffs.Add(buff.Key, buff.Value);
                }
            }
        }

        if (passives != null)
        {
            foreach (var effect in passives)
            {
                effects[effect] = null;
            }
        }
        
        newUnit.Buffs = buffs;

        var devices = playerInfo.Devices?.ConvertDevices(playerInfo.Perks ?? []);

        var updatedDevices = new Dictionary<int, DeviceData>();

        if (devices != null)
        {
            foreach (var device in devices)
            {
                var deviceCard = Databases.Catalogue.GetCard<CardDevice>(device.Value);
                var itemKey = deviceCard?.DeviceKeyAtLevel((byte?) playerInfo.DeviceLevels?[deviceCard.GroupKey] ?? 1);
                if (itemKey == null) continue;
                var itemCard = Databases.Catalogue.GetCard(itemKey.Value);
                var deviceData = itemCard switch
                {
                    CardBlock cBlock => new DeviceData
                    {
                        DeviceKey = device.Value,
                        TotalCost = BuffHelper.BuildCost(newUnit, cBlock.BaseCost ?? 1),
                        CostInc = BuffHelper.BuildCost(newUnit, cBlock.CostIncPerUnit ?? 0)
                    },
                    CardUnit cUnit => new DeviceData
                    {
                        DeviceKey = device.Value,
                        TotalCost = BuffHelper.BuildCost(newUnit, cUnit.BaseCost ?? 1),
                        CostInc = BuffHelper.BuildCost(newUnit, cUnit.CostIncPerUnit ?? 0)
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
                ? BuffHelper.UnitMaxHealth(newUnit, unitCard.Health.Health.MaxHealth)
                : null,
            Forcefield = unitCard.Health?.Forcefield != null
                ? BuffHelper.UnitMaxForcefield(newUnit, unitCard.Health.Forcefield.MaxAmount)
                : null,
            Shield = unitCard.Health?.Health?.Shield,
            MovementActive = false,
            CurrentGear = newUnit.Gears[0].Key,
            Ability = abilityCard?.Key,
            AbilityCharges = abilityCard?.Charges?.MaxCharges,
            AbilityChargeCooldownEnd = 0,
            Resource = gameInitiator.GetResourceAmount(),
            Effects = effects,
            Devices = devices != null ? updatedDevices : null
        };
        
        newUnit.UpdateData(initUpdate);
        return newUnit;
    }
}