using System.Numerics;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.ServerTypes;

public partial class GameZone
{
    private const ulong StaleRequestTimeout = 3000;
    
    public void ReceivedMoveRequest(uint unitId, ulong time, ZoneTransform transform)
    {
        if (!_units[unitId].UnitMove(transform, time))
        {
            _serviceZone.SendUnitMoveFail(unitId, _units[unitId].LastMoveUpdateTime);
        }
    }

    public void ReceivedBuildRequest(ushort rpcId, uint playerId, BuildInfo buildInfo, IServiceZone builderService)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player) ||
            !player.Devices.Values.Select(d => d.DeviceKey).Contains(buildInfo.DeviceKey))
        {
            builderService.SendStartBuild(rpcId, false);
            return;
        }
        
        player.CurrentBuildInfo = buildInfo;
        builderService.SendStartBuild(rpcId, true);
        _unbufferedZone.SendDoStartBuild(playerUnitId, buildInfo);
        
        var devCard = Databases.Catalogue.GetCard<CardDevice>(buildInfo.DeviceKey);
        var itemCard = devCard?.DeviceKeyAtLevel((byte)player.DeviceLevels[devCard.GroupKey]);
        var activateEffects = false;
        if (itemCard is not null)
        {
            activateEffects = Databases.Catalogue.GetCard(itemCard.Value) switch
            {
                CardBlock cardBlock => cardBlock.BuildTime > 0,
                CardUnit cardUnit => cardUnit.BuildTime > 0,
                _ => activateEffects
            };
        }

        if (!activateEffects) return;
        var buildEffects = player.ActiveEffects.GetEffectsOfType<ConstEffectOnBuilding>();
        foreach (var buildEffect in buildEffects)
        {
            if (buildEffect.ConstantEffects is { } constantEffects)
            {
                player.AddEffects(constantEffects.Select(k => new ConstEffectInfo(k)).ToList(), player.Team);
            }
        }
    }

    public void ReceivedCancelBuildRequest(uint playerId)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return;
        }

        player.CurrentBuildInfo = null;
        _unbufferedZone.SendDoCancelBuild(playerUnitId);
        
        var buildEffects = player.ActiveEffects.GetEffectsOfType<ConstEffectOnBuilding>();
        foreach (var buildEffect in buildEffects)
        {
            if (buildEffect.ConstantEffects is { } constantEffects)
            {
                player.RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)).ToList(), player.Team);
            }
        }
    }

    public void ReceivedEventBroadcast(ZoneEvent zoneEvent)
    {
        _serviceZone.SendBroadcastZoneEvent(zoneEvent);
    }

    public void ReceivedSwitchGearRequest(ushort rpcId, uint playerId, Key gearKey, IServiceZone switcherService)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return;
        }
        
        var gear = player.GetGearByKey(gearKey);
        if (gear is null || gear.IsOutOfAmmo())
        {
            switcherService.SendSwitchGear(rpcId, false);
            return;
        }
        
        switcherService.SendSwitchGear(rpcId, true);
        player.SetGear(gearKey);
    }

    public void ReceivedStartReloadRequest(ushort rpcId, uint playerId, IServiceZone reloaderService)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return;
        }

        if (!player.CurrentGear?.IsPossibleToReload() ?? true)
        {
            reloaderService.SendStartReload(rpcId, false);
            return;
        }
        
        reloaderService.SendStartReload(rpcId, true);
        _unbufferedZone.SendDoStartReload(playerUnitId);
        
        var reloadEffects = player.ActiveEffects.GetEffectsOfType<ConstEffectOnReload>();
        foreach (var reloadEffect in reloadEffects)
        {
            if (reloadEffect.ReloadStartEffect is { } startEffect)
            {
                ApplyInstEffect(player, [player], startEffect, false, player.CreateImpactData());
            }
            
            if (reloadEffect.ConstantEffects is { } constantEffects)
            {
                player.AddEffects(constantEffects.Select(k => new ConstEffectInfo(k)).ToList(), player.Team);
            }
        }
    }
    
    public void ReceivedReloadRequest(ushort rpcId, uint playerId, IServiceZone reloaderService)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return;
        }

        if (!player.CurrentGear?.IsPossibleToReload() ?? true)
        {
            reloaderService.SendReload(rpcId, false);
            _unbufferedZone.SendDoCancelReload(playerUnitId);
        }
        else
        {
            reloaderService.SendReload(rpcId, true);
            player.ReloadAmmo();
        }
        
        var reloadEffects = player.ActiveEffects.GetEffectsOfType<ConstEffectOnReload>();
        foreach (var reloadEffect in reloadEffects)
        {
            if (reloadEffect.ReloadEndEffect is { } endEffect)
            {
                ApplyInstEffect(player, [player], endEffect, false, player.CreateImpactData());
            }
            
            if (reloadEffect.ConstantEffects is { } constantEffects)
            {
                player.RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)).ToList(), player.Team);
            }
        }
    }

    public void ReceivedReloadEndRequest(uint playerId)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId))
        {
            return;
        }
        
        _unbufferedZone.SendDoEndReload(playerUnitId);
    }
    
    public void ReceivedReloadCancelRequest(uint playerId)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return;
        }

        _unbufferedZone.SendDoCancelReload(playerUnitId);
        var reloadEffects = player.ActiveEffects.GetEffectsOfType<ConstEffectOnReload>();
        foreach (var reloadEffect in reloadEffects)
        {
            if (reloadEffect.ReloadEndEffect is { } endEffect)
            {
                ApplyInstEffect(player, [player], endEffect, false, player.CreateImpactData());
            }
            
            if (reloadEffect.ConstantEffects is { } constantEffects)
            {
                player.RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)).ToList(), player.Team);
            }
        }
    }

    public void ReceivedCastRequest(uint playerId, CastData castData)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return;
        }

        if (castData.Shots is not { Count: > 0 } shots) return;
        
        var tool = player.CurrentGear?.Tools[castData.ToolIndex];
        
        if (!tool?.IsEnoughAmmoToUse() ?? false) return;

        foreach (var shot in shots.Where(shot => shot.ShotId.HasValue))
        {
            _shotInfo[shot.ShotId.Value] = new ShotInfo(shot.ShotId.Value, player, castData.ShotPos, shot.TargetPos,
                player.CurrentGear, castData.ToolIndex);
        }
        
        var ammoUpdate = tool?.TakeAmmoUpdate();
        if (ammoUpdate is not null && player.CurrentGear is not null)
        {
            player.UpdateData(new UnitUpdate
            {
                Ammo = new Dictionary<Key, List<Ammo>> { { player.CurrentGear.Key, [ammoUpdate]}}
            });
        }
        
        if (tool?.Tool is ToolBuild)
        {
            var buildEffects = player.ActiveEffects.GetEffectsOfType<ConstEffectOnBuilding>();
            foreach (var buildEffect in buildEffects)
            {
                if (buildEffect.ConstantEffects is { } constantEffects)
                {
                    player.RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)).ToList(), player.Team);
                }
            }
        }
    }

    public void ReceivedHit(ulong time, Dictionary<ulong, HitData> hits)
    {
        if ((ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds() > time + StaleRequestTimeout)
        {
            return;
        }

        foreach (var (shotId, hitData) in hits)
        {
            var shot = _shotInfo[shotId];
            var impactData = shot.Caster.CreateImpactDataExact(hitData.InsidePoint, shot.ShotPos, hitData.Normal,
                hitData.Crit ?? false, shot.SourceGear?.Key ?? shot.SourceAbility);

            if (shot.SourceGear is not null && shot.ToolIndex is not null)
            {
                switch (shot.SourceGear.Tools[shot.ToolIndex.Value].Tool)
                {
                    case ToolBuild toolBuild:
                        if (Vector3.DistanceSquared(shot.ShotPos, hitData.InsidePoint) > MathF.Pow(toolBuild.Range + 1, 2) || shot.Caster.CurrentBuildInfo is null)
                        {
                            return;
                        }
    
                        var buildInfo = shot.Caster.CurrentBuildInfo;
                        var devCard = Databases.Catalogue.GetCard<CardDevice>(buildInfo.DeviceKey);
                        var devData = shot.Caster.Devices.Values.First(d => d.DeviceKey == buildInfo.DeviceKey);
                        var instEffect = new InstEffectBuildDevice
                        {
                            Impact = toolBuild.BuildImpact,
                            DeviceKey = buildInfo.DeviceKey,
                            TotalCost = devData.TotalCost,
                            Level = shot.Caster.DeviceLevels.GetValueOrDefault(devCard?.GroupKey ?? Key.None, 1)
                        };
    
                        ApplyInstEffect(shot.Caster, [], instEffect, false, impactData, hitData.OutsideShift, buildInfo.Direction);
                        shot.Caster.CurrentBuildInfo = null;
                        break;
                    case ToolBurst toolBurst:
                        break;
                    case ToolChannel toolChannel:
                        break;
                    case ToolCharge toolCharge:
                        break;
                    case ToolDash toolDash:
                        break;
                    case ToolGroundSlam toolGroundSlam:
                        break;
                    case ToolMelee toolMelee:
                        break;
                    case ToolShot toolShot:
                        break;
                    case ToolSpinup toolSpinup:
                        break;
                    case ToolThrow toolThrow:
                        break;
                    default:
                        continue;
                }
            }
            else if (shot.SourceAbility is not null)
            {
                
            }
        }

        foreach (var (shotId, _) in hits)
        {
            if (!_keepShotAlive.Contains(shotId))
                _shotInfo.Remove(shotId);
        }
    }
}