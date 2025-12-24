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
        if (!_units.TryGetValue(unitId, out var unit))
        {
            return;
        }

        unit.LastMoveTime = DateTimeOffset.Now;
        unit.WasAfkWarned = false;
        if ((MapBinary.ContainsBlock((Vector3s)transform.Position) &&
             !MapBinary[(Vector3s)transform.Position].IsPassable) || !unit.UnitMove(transform, time))
        {
            _serviceZone.SendUnitMoveFail(unitId, unit.LastMoveUpdateTime);
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
        
        if (player.IsRecall)
        {
            player.EndRecall();
            _serviceZone.SendDoCancelRecall(playerUnitId);
        }

        if (player.HasSpawnProtection && _zoneData.MatchCard.RespawnLogic?.BreakProtectionOnAction is true)
        {
            player.SpawnProtectionTime = null;
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
        var impact = player.CreateImpactData();
        foreach (var buildEffect in buildEffects)
        {
            if (buildEffect.ConstantEffects is { } constantEffects)
            {
                player.AddEffects(constantEffects.Select(k => new ConstEffectInfo(k)), player.Team, player.GetSelfSource(impact));
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
                player.RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)), player.Team, player.GetSelfSource());
            }
        }
    }

    public void ReceivedEventBroadcast(ZoneEvent zoneEvent)
    {
        if (zoneEvent is ZoneEventUnitCommonLand commonLand && _units.TryGetValue(commonLand.UnitId, out var unit) && unit.Controlled)
        {
            if (!unit.IsFirstLand)
            {
                return;
            }

            unit.IsFirstLand = false;
        }

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
        
        if (player.HasSpawnProtection && _zoneData.MatchCard.RespawnLogic?.BreakProtectionOnAction is true)
        {
            player.SpawnProtectionTime = null;
        }
        
        switcherService.SendSwitchGear(rpcId, true);
        
        if (player.CurrentGear?.Card.EquipEffects is { Count: > 0 } effects1)
        {
            player.RemoveEffects(effects1.Select(e => new ConstEffectInfo(e, null)), player.Team, player.GetSelfSource());
        }

        if (gear.Card.EquipEffects is { Count: > 0 } effects2)
        {
            var impact = player.CreateImpactData();
            player.AddEffects(effects2.Select(e => new ConstEffectInfo(e)), player.Team, player.GetSelfSource(impact));
        }
        
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
        
        if (player.HasSpawnProtection && _zoneData.MatchCard.RespawnLogic?.BreakProtectionOnAction is true)
        {
            player.SpawnProtectionTime = null;
        }
        
        reloaderService.SendStartReload(rpcId, true);
        _unbufferedZone.SendDoStartReload(playerUnitId);
        
        var reloadEffects = player.ActiveEffects.GetEffectsOfType<ConstEffectOnReload>();
        var impact = player.CreateImpactData();
        var source = player.GetSelfSource(impact);
        foreach (var reloadEffect in reloadEffects)
        {
            if (reloadEffect.ReloadStartEffect is { } startEffect)
            {
                ApplyInstEffect(source, [player], startEffect, impact);
            }
            
            if (reloadEffect.ConstantEffects is { } constantEffects)
            {
                player.AddEffects(constantEffects.Select(k => new ConstEffectInfo(k)), player.Team, source);
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
            // We may have desynced ammo, so update it accordingly
            if (player.CurrentGear is not null)
            {
                player.UpdateData(new UnitUpdate
                {
                    Ammo = new Dictionary<Key, List<Ammo>> 
                    {
                        { 
                            player.CurrentGear.Key,
                            player.CurrentGear.Ammo
                                .Select(ammo => new Ammo { Index = ammo.AmmoIndex, Mag = ammo.Mag, Pool = ammo.Pool }).ToList()
                        }
                    }
                });
            }
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
            var impact = player.CreateImpactData();
            var playerSource = player.GetSelfSource(impact);
            if (reloadEffect.ReloadEndEffect is { } endEffect)
            {
                ApplyInstEffect(playerSource, [player], endEffect, impact);
            }
            
            if (reloadEffect.ConstantEffects is { } constantEffects)
            {
                player.RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)), player.Team, player.GetSelfSource());
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
        foreach (var reloadEffect in player.ActiveEffects.GetEffectsOfType<ConstEffectOnReload>())
        {
            var impact = player.CreateImpactData();
            var playerSource = player.GetSelfSource(impact);
            if (reloadEffect.ReloadEndEffect is { } endEffect)
            {
                ApplyInstEffect(playerSource, [player], endEffect, impact);
            }
            
            if (reloadEffect.ConstantEffects is { } constantEffects)
            {
                player.RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)), player.Team, player.GetSelfSource());
            }
        }
    }

    public void ReceivedProjCreateRequest(ulong shotId, ProjectileInfo projectileInfo, Guid? creatingSession)
    {
        if (projectileInfo.ProjectileKey.GetCard<CardProjectile>() is
            { Behaviour: ProjectileBehaviourGrenade { CollisionMask.Ground: true } })
        {
            _checkForWater.Add(shotId);
        }
        else
        {
            _keepShotAlive.Add(shotId);
        }
        _unbufferedZone.SendCreateProjectile(shotId, projectileInfo, creatingSession);
    }

    public void ReceivedProjMoveRequest(ulong shotId, ulong time, ZoneTransform zoneTransform)
    {
        if (_checkForWater.Contains(shotId) && zoneTransform.Position.Y < _zoneData.PlanePosition)
        {
            ReceivedHit(time, new Dictionary<ulong, HitData>
            {
                {
                    shotId, new HitData
                    {
                        InsidePoint = zoneTransform.Position,
                        OutsideShift = BlockShift.None,
                        Normal = Vector3s.Zero,
                        TargetId = null,
                        Crit = false
                    }
                }
            });
            ReceivedProjDropRequest(shotId);
        }
        else
        {
            _serviceZone.SendMoveProjectile(shotId, time, zoneTransform);
        }
    }

    public void ReceivedProjDropRequest(ulong shotId)
    {
        _keepShotAlive.Remove(shotId);
        _checkForWater.Remove(shotId);
        _shotInfo.Remove(shotId);
        _serviceZone.SendDropProjectile(shotId);
    }

    public void ReceivedCreateChannelRequest(ushort rpcId, uint playerId, ChannelData channelData, IServiceZone channelService)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player) || player.IsDead)
        {
            channelService.SendStartChannel(rpcId, false);
            return;
        }

        if (player.CurrentGear?.Tools[channelData.ToolIndex] is { } toolLogic && toolLogic.IsEnoughAmmoToUse() &&
            toolLogic.Tool is ToolChannel channel)
        {
            player.CurrentChannelData = channelData;
            player.TicksPerChannel = (ulong)MathF.Max(float.Round(channel.Interval / SecondsPerTick), 0.0f);
            if (player.IsRecall)
            {
                player.EndRecall();
                _serviceZone.SendDoCancelRecall(playerUnitId);
            }
            if (player.HasSpawnProtection && _zoneData.MatchCard.RespawnLogic?.BreakProtectionOnAction is true)
            {
                player.SpawnProtectionTime = null;
            }
            channelService.SendStartChannel(rpcId, true);
            _unbufferedZone.SendDoStartChannel(playerUnitId, channelData);
            var ammoUpdate = toolLogic.TakeAmmoUpdate();
            if (ammoUpdate is not null)
            {
                player.UpdateData(new UnitUpdate
                {
                    Ammo = new Dictionary<Key, List<Ammo>> { {player.CurrentGear.Key, [ammoUpdate]} }
                });
            }
            if (channelData.TargetUnit.HasValue && channel.ConstantEffects is { Count: > 0 } &&
                _units.TryGetValue(channelData.TargetUnit.Value, out var unit))
            {
                var impactData = player.CreateImpactData(insidePoint: channelData.HitPos, sourceKey: player.CurrentGear.Key);
                unit.AddEffects(channel.ConstantEffects.Select(k => new ConstEffectInfo(k)), player.Team, player.GetSelfSource(impactData));
            }
        }
        else
        {
            channelService.SendStartChannel(rpcId, false);
        }
    }

    public void ReceivedEndChannelRequest(uint playerId)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return;
        }

        if (player.CurrentChannelData is {} channelData)
        {
            _unbufferedZone.SendDoEndChannel(playerUnitId, channelData.ToolIndex);
            if (player.CurrentGear?.Tools[channelData.ToolIndex] is { Tool: ToolChannel channel } &&
                channelData.TargetUnit.HasValue && channel.ConstantEffects is { Count: > 0 } &&
                _units.TryGetValue(channelData.TargetUnit.Value, out var unit))
            {
                unit.RemoveEffects(channel.ConstantEffects.Select(k => new ConstEffectInfo(k, null)),
                    player.Team, player.GetSelfSource());    
            }
        }

        player.CurrentChannelData = null;
    }

    public void ReceivedToolChargeStartRequest(uint playerId, byte toolIndex)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return;
        }

        if (player.CurrentGear?.Tools[toolIndex] is { } toolLogic && toolLogic.IsEnoughAmmoToUse() &&
            toolLogic.Tool is ToolCharge)
        {
            player.StartChargeTime = DateTimeOffset.Now;
            _unbufferedZone.SendDoToolStartCharge(playerUnitId, toolIndex);
        }
    }

    public void ReceivedToolChargeEndRequest(ushort rpcId, uint playerId, byte toolIndex, IServiceZone chargeService)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            _unbufferedZone.SendToolEndChargeFail(rpcId, "not a player");
            return;
        }
        
        if (player.CurrentGear?.Tools[toolIndex].Tool is ToolCharge)
        {
            chargeService.SendToolEndChargeSuccess(rpcId, true, player.LengthOfCharge);
            _unbufferedZone.SendDoToolEndCharge(playerUnitId, toolIndex);
        }
        else
        {
            chargeService.SendToolEndChargeSuccess(rpcId, false, null);
        }
        
        player.StartChargeTime = null;
    }

    private const float DashImprecision = 0.1f;

    public void ReceivedDashChargeStartRequest(uint playerId, byte toolIndex)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return;
        }

        if (player.CurrentGear?.Tools[toolIndex] is { } toolLogic && toolLogic.IsEnoughAmmoToUse() &&
            toolLogic.Tool is ToolDash)
        {
            player.StartChargeTime = DateTimeOffset.Now;
            _unbufferedZone.SendDoDashStartCharge(playerUnitId, toolIndex);
        }
    }

    public void ReceivedDashChargeEndRequest(ushort rpcId, uint playerId, byte toolIndex, IServiceZone dashService)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            dashService.SendDashEndChargeFail(rpcId, "not a player");
            return;
        }
        
        if (player.CurrentGear?.Tools[toolIndex].Tool is ToolDash tool)
        {
            player.LastDashChargeMax = player.LengthOfCharge >= tool.MaxChargeTime - DashImprecision;
            dashService.SendDashEndChargeSuccess(rpcId, player.LastDashChargeMax);
            _unbufferedZone.SendDoDashEndCharge(playerUnitId, toolIndex);
        }
        else
        {
            dashService.SendDashEndChargeFail(rpcId, "not a dash tool");
        }
        
        player.StartChargeTime = null;
    }

    public void ReceivedDashCastRequest(uint playerId, byte toolIndex)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player) || player.IsDead)
        {
            return;
        }
        
        var tool = player.CurrentGear?.Tools[toolIndex];
        
        if (tool?.Tool is not ToolDash toolDash || !tool.IsEnoughAmmoToUse() || player.IsBuff(BuffType.Root)) return;
        
        if (player.IsRecall)
        {
            player.EndRecall();
            _serviceZone.SendDoCancelRecall(playerUnitId);
        }
        if (player.HasSpawnProtection && _zoneData.MatchCard.RespawnLogic?.BreakProtectionOnAction is true)
        {
            player.SpawnProtectionTime = null;
        }
        
        _serviceZone.SendCast(playerUnitId, new CastData
        {
            ShotPos = player.Transform.Position,
            ToolIndex = toolIndex,
            Shots = []
        });
        
        var ammoUpdate = tool.TakeAmmoUpdate(toolDash.Ammo?.Rate *
                                             (player.LastDashChargeMax
                                                 ? toolDash.MaxAmmoRateMultiplier
                                                 : toolDash.MinAmmoRateMultiplier));
        if (ammoUpdate is not null && player.CurrentGear is not null)
        {
            player.UpdateData(new UnitUpdate
            {
                Ammo = new Dictionary<Key, List<Ammo>> { { player.CurrentGear.Key, [ammoUpdate]}}
            });
        }
    }

    public void ReceivedDashHitRequest(uint playerId, byte toolIndex, HitData hitData)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return;
        }

        if (player.CurrentGear?.Tools[toolIndex].Tool is not ToolDash tool) return;
        var impactData = player.CreateImpactData(hitData.InsidePoint, player.Transform.Position, hitData.Normal,
            hitData.Crit ?? false, player.CurrentGear.Key);
            
        Unit? hitTarget = null;
        if (hitData.TargetId is not null && _units.TryGetValue(hitData.TargetId.Value, out var target))
        {
            hitTarget = target;
        }
            
        if (player.LastDashChargeMax && tool is { MaxHitEffect: not null })
        {
            ApplyInstEffect(player.GetSelfSource(impactData), hitTarget is not null ? [hitTarget] : [],
                tool.MaxHitEffect, impactData, hitData.OutsideShift, hitData.Direction);
        }
        else if (tool is { HitEffect: not null })
        {
            ApplyInstEffect(player.GetSelfSource(impactData), hitTarget is not null ? [hitTarget] : [],
                tool.HitEffect, impactData, hitData.OutsideShift, hitData.Direction);
        }
    }

    public void ReceivedGroundSlamCastRequest(uint playerId, byte toolIndex)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player)
            || player.IsDead)
        {
            return;
        }
        
        var tool = player.CurrentGear?.Tools[toolIndex];
        
        if (tool?.Tool is not ToolGroundSlam toolSlam || !tool.IsEnoughAmmoToUse()) return;
        
        if (player.IsRecall)
        {
            player.EndRecall();
            _serviceZone.SendDoCancelRecall(playerUnitId);
        }
        if (player.HasSpawnProtection && _zoneData.MatchCard.RespawnLogic?.BreakProtectionOnAction is true)
        {
            player.SpawnProtectionTime = null;
        }

        _serviceZone.SendDoGroundSlamCast(playerUnitId, toolIndex);
        
        var ammoUpdate = tool.TakeAmmoUpdate(toolSlam.Ammo?.Rate);
        if (ammoUpdate is not null && player.CurrentGear is not null)
        {
            player.UpdateData(new UnitUpdate
            {
                Ammo = new Dictionary<Key, List<Ammo>> { { player.CurrentGear.Key, [ammoUpdate]}}
            });
        }
    }

    public void ReceivedGroundSlamHitRequest(uint playerId, byte toolIndex, HitData hitData)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return;
        }

        if (player.CurrentGear?.Tools[toolIndex].Tool is not ToolGroundSlam tool) return;
        var impactData = player.CreateImpactData(hitData.InsidePoint, player.Transform.Position, hitData.Normal,
            hitData.Crit ?? false, player.CurrentGear.Key);
            
        Unit? hitTarget = null;
        if (hitData.TargetId is not null && _units.TryGetValue(hitData.TargetId.Value, out var target))
        {
            hitTarget = target;
        }
            
        if (tool is { HitEffect: not null })
        {
            ApplyInstEffect(player.GetSelfSource(impactData), hitTarget is not null ? [hitTarget] : [],
                tool.HitEffect, impactData, hitData.OutsideShift, hitData.Direction);
        }
    }

    public void ReceivedAbilityCastRequest(ushort rpcId, uint playerId, AbilityCastData castData, IServiceZone abilityService)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player) || player.AbilityCharges <= 0 ||
            player.AbilityCard is not { Behavior: not null } aCard
            || player.IsDead)
        {
            abilityService.SendCastAbility(rpcId, false);
            return;
        }

        if (player.AbilityTriggered)
        {
            player.AbilityTriggered = false;
            player.AbilityTriggerTimeEnd = null;
            player.AbilityUsed();
            abilityService.SendCastAbility(rpcId, true);
            _serviceZone.SendDoCastAbility(playerUnitId, castData);
            player.RemoveTriggerEffects();
            return;
        }

        if (ValidateAbility(player) && !player.IsBuff(BuffType.Disabled))
        {
            if (player.IsRecall)
            {
                player.EndRecall();
                _serviceZone.SendDoCancelRecall(playerUnitId);
            }
            if (player.HasSpawnProtection && _zoneData.MatchCard.RespawnLogic?.BreakProtectionOnAction is true)
            {
                player.SpawnProtectionTime = null;
            }
            
            abilityService.SendCastAbility(rpcId, true);
            _serviceZone.SendDoCastAbility(playerUnitId, castData);
            switch (aCard.Behavior)
            {
                case AbilityBehaviorCast abilityBehaviorCast:
                    player.AbilityUsed();
                    if (castData.Shots is { Count: > 0 } shots)
                    {
                        switch (abilityBehaviorCast.Application)
                        {
                            case AbilityApplicationUnitProjectile abilityApplicationUnitProjectile:
                                shots.ForEach(shot =>
                                {
                                    CreateProjectileUnit(abilityApplicationUnitProjectile.UnitProjectileKey,
                                        abilityApplicationUnitProjectile.Speed, shot,
                                        castData.ShotPos ?? player.Transform.Position, player);
                                });
                                break;
                            
                            default:
                                shots.ForEach(shot =>
                                {
                                    if (shot.ShotId.HasValue)
                                    {
                                        _shotInfo[shot.ShotId.Value] = new ShotInfo(shot.ShotId.Value, player,
                                            castData.ShotPos ?? player.Transform.Position, shot.TargetPos,
                                            SourceAbility: player.AbilityKey);
                                    }
                                });
                                break;
                        }
                    }
                    break;
                case AbilityBehaviorTrigger abilityBehaviorTrigger:
                    player.AbilityTriggered = true;
                    if (abilityBehaviorTrigger.MaxDuration.HasValue)
                    {
                        player.AbilityTriggerTimeEnd =
                            DateTimeOffset.Now.AddSeconds(abilityBehaviorTrigger.MaxDuration.Value);
                    }

                    var impactData = player.CreateImpactData(shotPos: castData.ShotPos, sourceKey: player.AbilityKey);

                    if (abilityBehaviorTrigger.TriggerEffects is { Count: > 0 } effects)
                    {
                        player.AddEffects(effects.Select(e => new ConstEffectInfo(e)), player.Team,
                            new TriggerSource(player, impactData));
                    }
                    break;
            }
        }
        else
        {
            abilityService.SendCastAbility(rpcId, false);
        }
    }

    public void ReceivedUnitProjectileHit(uint unitId, HitData hitData)
    {
        if (!_units.TryGetValue(unitId, out var projectile) || projectile.ProjectileUnitData is not { } projectileData)
        {
            return;
        }

        var impactData = projectile.CreateImpactDataExact(insidePoint: hitData.InsidePoint, normal: hitData.Normal,
            crit: hitData.Crit ?? false);
        if (hitData.TargetId != null && _units.TryGetValue(hitData.TargetId.Value, out var target) && target.PlayerId != null)
        {
            if (projectileData.PlayerCollisionEffect is not null)
            {
                ApplyInstEffect(projectile.GetSelfSource(impactData), [target], projectileData.PlayerCollisionEffect,
                    impactData, hitData.OutsideShift, hitData.Direction);
            }

            if (projectileData.DieOnPlayerCollision)
            {
                projectile.Killed(impactData);
            }
        }
        else
        {
            if (projectileData.WorldCollisionEffect is not null)
            {
                ApplyInstEffect(projectile.GetSelfSource(impactData), [], projectileData.WorldCollisionEffect,
                    impactData, hitData.OutsideShift, hitData.Direction);
            }

            if (projectileData.DieOnWorldCollision)
            {
                projectile.Killed(impactData);
            }
        }
    }

    public void ReceivedSkybeamHit(uint unitId, HitData hitData)
    {
        if (!_units.TryGetValue(unitId, out var skybeam) || skybeam.SkybeamUnitData is not { } skybeamData ||
            skybeamData.HitEffect is null)
        {
            return;
        }

        Unit? hitTarget = null;
        if (hitData.TargetId is not null && _units.TryGetValue(hitData.TargetId.Value, out var target))
        {
            hitTarget = target;
        }
        
        var impactData = skybeam.CreateImpactDataExact(insidePoint: hitData.InsidePoint, normal: hitData.Normal,
            crit: hitData.Crit ?? false);

        ApplyInstEffect(skybeam.GetSelfSource(impactData), hitTarget is not null ? [hitTarget] : [],
            skybeamData.HitEffect, impactData, hitData.OutsideShift, hitData.Direction);
    }

    public void ReceivedCastRequest(uint playerId, CastData castData)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player)
            || player.IsDead)
        {
            return;
        }
        
        if (player.IsRecall)
        {
            player.EndRecall();
            _serviceZone.SendDoCancelRecall(playerUnitId);
        }
        if (player.HasSpawnProtection && _zoneData.MatchCard.RespawnLogic?.BreakProtectionOnAction is true)
        {
            player.SpawnProtectionTime = null;
        }
        
        if (player.IsBuff(BuffType.Disarm))
        {
            return;
        }
        
        _serviceZone.SendCast(playerUnitId, castData);

        if (castData.Shots is not { Count: > 0 } shots) return;
        
        var tool = player.CurrentGear?.Tools[castData.ToolIndex];
        
        if (!tool?.IsEnoughAmmoToUse() ?? false) return;
        
        shots.ForEach(shot =>
        {
            if (castData.UnitProjectileSpeed is not null)
            {
                switch (tool?.Tool)
                {
                    case ToolBurst { Bullet.Count: > 0 } toolBurst when toolBurst.Bullet[0] is ToolBulletUnitProjectile burstBullet:
                        CreateProjectileUnit(burstBullet.UnitProjectileKey, castData.UnitProjectileSpeed.Value, shot, castData.ShotPos, player);
                        break;
                    
                    case ToolCharge { MaxOptions.Bullet: ToolBulletUnitProjectile chargeBullet } toolChargeMax
                        when player.LengthOfCharge >= toolChargeMax.MaxChargeTime:
                        CreateProjectileUnit(chargeBullet.UnitProjectileKey, castData.UnitProjectileSpeed.Value, shot, castData.ShotPos, player);
                        break;
                    
                    case ToolCharge { MinOptions.Bullet: ToolBulletUnitProjectile chargeBullet }:
                        CreateProjectileUnit(chargeBullet.UnitProjectileKey, castData.UnitProjectileSpeed.Value, shot, castData.ShotPos, player);
                        break;
                    
                    case ToolShot { Bullet: ToolBulletUnitProjectile shotBullet }:
                        CreateProjectileUnit(shotBullet.UnitProjectileKey, castData.UnitProjectileSpeed.Value, shot, castData.ShotPos, player);
                        break;
                    
                    case ToolSpinup { Bullet: ToolBulletUnitProjectile spinupBullet }:
                        CreateProjectileUnit(spinupBullet.UnitProjectileKey, castData.UnitProjectileSpeed.Value, shot, castData.ShotPos, player);
                        break;
                    
                    case ToolThrow { Bullet: ToolBulletUnitProjectile throwBullet }:
                        CreateProjectileUnit(throwBullet.UnitProjectileKey, castData.UnitProjectileSpeed.Value, shot, castData.ShotPos, player);
                        break;
                }
            }
            else if (shot.ShotId.HasValue)
            {
                _shotInfo[shot.ShotId.Value] = tool?.Tool switch
                {
                    ToolCharge => new ShotInfo(shot.ShotId.Value, player, castData.ShotPos, shot.TargetPos,
                        player.CurrentGear, castData.ToolIndex, ChargeLength: player.LengthOfCharge),

                    _ => new ShotInfo(shot.ShotId.Value, player, castData.ShotPos, shot.TargetPos,
                        player.CurrentGear, castData.ToolIndex)
                };
            }
        });
        
        var ammoUpdate = tool?.TakeAmmoUpdate();
        if (ammoUpdate is not null && player.CurrentGear is not null)
        {
            player.UpdateData(new UnitUpdate
            {
                Ammo = new Dictionary<Key, List<Ammo>> { { player.CurrentGear.Key, [ammoUpdate]}}
            });
        }

        if (tool?.Tool is not ToolBuild) return;
        foreach (var buildEffect in player.ActiveEffects.GetEffectsOfType<ConstEffectOnBuilding>())
        {
            if (buildEffect.ConstantEffects is { } constantEffects)
            {
                player.RemoveEffects(constantEffects.Select(k => new ConstEffectInfo(k)), player.Team, player.GetSelfSource());
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
            if (!_shotInfo.TryGetValue(shotId, out var shot)) continue;
            var impactData = shot.Caster.CreateImpactDataExact(hitData.InsidePoint, shot.ShotPos, hitData.Normal,
                hitData.Crit ?? false, shot.SourceGear?.Key ?? shot.SourceAbility);
            var casterSource = shot.Caster.GetSelfSource(impactData);
            
            Unit? hitTarget = null;
            if (hitData.TargetId is not null && _units.TryGetValue(hitData.TargetId.Value, out var target))
            {
                hitTarget = target;
            }

            if (shot.SourceGear is not null && shot.ToolIndex is not null && !casterSource.Unit.IsDead)
            {
                switch (shot.SourceGear.Tools[shot.ToolIndex.Value].Tool)
                {
                    case ToolBuild toolBuild:
                        if (shot.Caster.CurrentBuildInfo is null ||
                            Vector3.DistanceSquared(shot.ShotPos, hitData.InsidePoint) >
                            MathF.Pow(toolBuild.Range + 1, 2))
                        {
                            continue;
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
    
                        ApplyInstEffect(casterSource, [], instEffect, impactData, hitData.OutsideShift,
                            buildInfo.Direction);
                        shot.Caster.CurrentBuildInfo = null;
                        break;
                    
                    case ToolBurst toolBurst:
                        if (toolBurst.HitEffect is null /* ||
                             Vector3.DistanceSquared(shot.ShotPos, hitData.InsidePoint) > MathF.Pow(toolBurst.Range + 1, 2) */
                           )
                        {
                            continue;
                        }

                        ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [],
                            toolBurst.HitEffect, impactData, hitData.OutsideShift, hitData.Direction);
                        break;
                    
                    case ToolCharge toolCharge:
                        if (shot.ChargeLength >= toolCharge.MaxChargeTime && toolCharge.MaxOptions.HitEffect is not null)
                        {
                            ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [],
                                toolCharge.MaxOptions.HitEffect, impactData, hitData.OutsideShift, hitData.Direction);
                        }
                        else if (toolCharge.MinOptions.HitEffect is not null)
                        {
                            ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [],
                                toolCharge.MinOptions.HitEffect, impactData, hitData.OutsideShift, hitData.Direction); 
                        }
                        break;
                    
                    case ToolGroundSlam toolGroundSlam:
                        if (toolGroundSlam.HitEffect is null)
                        {
                            continue;
                        }

                        ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [],
                            toolGroundSlam.HitEffect, impactData, hitData.OutsideShift, hitData.Direction);
                        break;
                    
                    case ToolMelee toolMelee:
                        if (toolMelee.HitEffect is null || 
                            Vector3.DistanceSquared(shot.ShotPos, hitData.InsidePoint) > MathF.Pow(toolMelee.Range + 1, 2))
                        {
                            continue;
                        }

                        ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [],
                            toolMelee.HitEffect, impactData, hitData.OutsideShift, hitData.Direction);
                        break;
                    
                    case ToolShot toolShot:
                        if (toolShot.HitEffect is null /* ||
                             Vector3.DistanceSquared(shot.ShotPos, hitData.InsidePoint) > MathF.Pow(toolShot.Range + 1, 2) */
                            )
                        {
                            continue;
                        }

                        ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [],
                            toolShot.HitEffect, impactData, hitData.OutsideShift, hitData.Direction);
                        break;
                    
                    case ToolSpinup toolSpinup:
                        if (toolSpinup.HitEffect is null /* ||
                             Vector3.DistanceSquared(shot.ShotPos, hitData.InsidePoint) > MathF.Pow(toolSpinup.Range + 1, 2) */
                            )
                        {
                            continue;
                        }

                        ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [],
                            toolSpinup.HitEffect, impactData, hitData.OutsideShift, hitData.Direction);
                        break;
                    
                    case ToolThrow toolThrow:
                        if (toolThrow.HitEffect is null)
                        {
                            continue;
                        }

                        ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [],
                            toolThrow.HitEffect, impactData, hitData.OutsideShift, hitData.Direction);
                        break;
                    
                    default:
                        continue;
                }
            }
            else if (shot.SourceAbility is not null)
            {
                var abilityCard = Databases.Catalogue.GetCard<CardAbility>(shot.SourceAbility.Value);
                if (abilityCard is null) continue;

                if (abilityCard.Behavior is AbilityBehaviorCast { HitEffect: not null } castBehavior)
                {
                    ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [],
                        castBehavior.HitEffect, impactData, hitData.OutsideShift, hitData.Direction);
                }
            }
            else if (shot.Caster.TurretUnitData is { HitEffect: not null } turretData)
            {
                ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [], turretData.HitEffect,
                    impactData, hitData.OutsideShift, hitData.Direction);
            }
            else if (shot.Caster is { MortarUnitData: not null, OnMortarHit: not null })
            {
                ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [], shot.Caster.OnMortarHit,
                    impactData, hitData.OutsideShift, hitData.Direction);
            }
            else if (shot.Caster.DrillUnitData is { HitEffect: not null } drillData)
            {
                ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [], drillData.HitEffect, impactData,
                    hitData.OutsideShift, hitData.Direction);
            }
            else if (shot.Caster.TeslaUnitData is { HitEffect: not null } teslaData)
            {
                ApplyInstEffect(casterSource, hitTarget is not null ? [hitTarget] : [], teslaData.HitEffect, impactData,
                    hitData.OutsideShift, hitData.Direction);
            }
        }

        foreach (var (shotId, _) in hits)
        {
            if (!_keepShotAlive.Contains(shotId)) 
                _shotInfo.Remove(shotId);
        }
    }

    public void ReceivedFall(uint unitId, float height, bool force)
    {
        if (!_units.TryGetValue(unitId, out var unit))
        {
            return;
        }

        var fallPos = unit.GetFallPosition();
        var blockCheckPos = (Vector3s)(fallPos with { Y = fallPos.Y - 1 });
        var fallMin = _zoneData.MapData.Properties?.MinFallHeight ?? 5f;
        var fallMax = _zoneData.MapData.Properties?.MaxFallHeight ?? 25f;
        if (MapBinary.ContainsBlock(blockCheckPos) && MapBinary[blockCheckPos].Card.Special is BlockSpecialNoFallDamage)
        {
            unit.OnFall(height, force, fallMin, fallMax, false);
        }
        else
        {
            unit.OnFall(height, force, fallMin, fallMax, true);
        }
    }

    public void ReceivedPickup(uint playerId, uint pickupId)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player) ||
            !_units.TryGetValue(pickupId, out var pickup) ||
            pickup.PickupUnitData is null ||
            !pickup.CanPickUp ||
            player.IsDead)
        {
            return;
        }
        
        _serviceZone.SendPickupTaken(playerId, pickup.Key);
        pickup.PickupTaken(player);
    }

    public void ReceivedSelectSpawnPoint(uint playerId, uint? spawnId)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
                !_playerUnits.TryGetValue(playerUnitId, out var player) ||
                (spawnId is not null && !_zoneData.SpawnPoints.ContainsKey(spawnId.Value)))
        {
            return;
        }
        
        _zoneData.UpdatePlayerSelectedSpawn(playerId, spawnId);
    }

    public void ReceivedTurretTarget(uint playerId, uint turretId, uint targetId)
    {
        if (!_units.TryGetValue(turretId, out var turret) || turret.TurretUnitData is null ||
            !_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player) ||
            (targetId != 0U && (!_units.TryGetValue(targetId, out var target) || 
                                (turret.Controlled && targetId != player.Id))))
        {
            return;
        }
        
        turret.SetTurretTarget(targetId);
    }

    public void ReceivedTurretAttack(uint playerId, uint turretId, Vector3 shotPos, List<ShotData> shots)
    {
        if (!_units.TryGetValue(turretId, out var turret) || turret.TurretUnitData is null ||
            !_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player) ||
            (turret.TurretUnitData.RequiresTarget && (turret.TurretTargetId == 0U ||
            (turret.Controlled && turret.TurretTargetId != player.Id))))
        {
            return;
        }
        
        _serviceZone.SendTurretAttack(turretId, shotPos, shots);
        
        if (shots is not { Count: > 0 }) return;
        
        shots.ForEach(shot =>
        {
            if (shot.ShotId.HasValue)
            {
                _shotInfo[shot.ShotId.Value] = new ShotInfo(shot.ShotId.Value, turret, shotPos, shot.TargetPos);
            }
        });
    }

    public void ReceivedMortarAttack(uint mortarId, Vector3 shotPos, List<ShotData> shots)
    {
        if (!_units.TryGetValue(mortarId, out var mortar) || mortar.MortarUnitData is null)
        {
            return;
        }
        
        _serviceZone.SendMortarAttack(mortarId, shotPos, shots);
        
        if (shots is not { Count: > 0 }) return;
        
        shots.ForEach(shot =>
        {
            if (shot.ShotId.HasValue)
            {
                _shotInfo[shot.ShotId.Value] = new ShotInfo(shot.ShotId.Value, mortar, shotPos, shot.TargetPos);
            }
        });
    }

    public void ReceivedDrillAttack(uint drillId, Vector3 shotPos, List<ShotData> shots)
    {
        if (!_units.TryGetValue(drillId, out var drill) || drill.DrillUnitData is null)
        {
            return;
        }
        
        _serviceZone.SendDrillAttack(drillId, shotPos, shots);
        drill.HitCount += 1;
        
        if (shots is not { Count: > 0 }) return;
        shots.ForEach(shot =>
        {
            if (shot.ShotId.HasValue)
            {
                _shotInfo[shot.ShotId.Value] = new ShotInfo(shot.ShotId.Value, drill, shotPos, shot.TargetPos);
            }
        });
    }

    public void ReceivedUpdateTesla(uint teslaId, uint? targetId, List<uint> teslasInRange)
    {
        if (!_units.TryGetValue(teslaId, out var tesla) || tesla.TeslaUnitData is null)
        {
            return;
        }
        
        var actualPropTeslas = teslasInRange.Select(t => _units.GetValueOrDefault(t)).OfType<Unit>()
            .Where(u => u.TeslaUnitData is not null).ToList();
        
        var firstCharged = tesla.TeslaCharge is TeslaChargeType.SelfCharge or TeslaChargeType.FullSelfCharge
            ? tesla
            : actualPropTeslas.FirstOrDefault(u =>
                u.TeslaCharge is TeslaChargeType.SelfCharge or TeslaChargeType.FullSelfCharge);
        
        if (firstCharged is null) return;

        if (targetId is null && tesla.TeslaCharge == TeslaChargeType.NoCharge)
        {
            tesla.UpdateData(new UnitUpdate
            {
                TeslaCharge = TeslaChargeType.RemoteCharge
            });
        }
        else if (targetId.HasValue)
        {
            if (!_units.TryGetValue(targetId.Value, out var target))
            {
                return;
            }
            
            if (firstCharged.Id != tesla.Id)
            {
                var noCharge = new UnitUpdate
                {
                    TeslaCharge = TeslaChargeType.NoCharge
                };
                actualPropTeslas.ForEach(u =>
                {
                    if (u.TeslaCharge is TeslaChargeType.RemoteCharge)
                    {
                        tesla.UpdateData(noCharge); 
                    }

                    if (u.TeslaUnitData?.PropagationEffect is null) return;
                    var impact = u.CreateBlankImpactData();
                    var source = u.GetSelfSource(impact);
                    ApplyInstEffect(source, [u], u.TeslaUnitData.PropagationEffect, impact);
                });
                
                if (tesla.TeslaCharge is TeslaChargeType.RemoteCharge)
                {
                    tesla.UpdateData(noCharge); 
                }

                if (tesla.TeslaUnitData?.PropagationEffect is not null)
                {
                    var impact = tesla.CreateBlankImpactData();
                    var source = tesla.GetSelfSource(impact);
                    ApplyInstEffect(source, [tesla], tesla.TeslaUnitData.PropagationEffect, impact);
                }
            }
            
            firstCharged.ChargeTesla(-1);
            if (firstCharged.TeslaUnitData?.AttackEffect is not null)
            {
                var impact = firstCharged.CreateBlankImpactData();
                var source = firstCharged.GetSelfSource(impact);
                ApplyInstEffect(source, [firstCharged], firstCharged.TeslaUnitData.AttackEffect, impact);
            }

            _serviceZone.SendTeslaAttack(teslaId, targetId.Value, actualPropTeslas.Select(u => u.Id).ToList());
            if (tesla.TeslaUnitData?.HitEffect is not null)
            {
                var impact = tesla.CreateImpactData(insidePoint: target.GetMidpoint());
                var source = tesla.GetSelfSource(impact);
                ApplyInstEffect(source, [target], tesla.TeslaUnitData.HitEffect, impact);
            }
        }
    }

    public void ReceivedPlayerCommand(uint playerId, Key command) => _serviceZone.SendPlayerCommand(playerId, command);

    public void ReceivedStartRecallRequest(uint playerId)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player) ||
            player is not { IsDead: false, IsActive: true, IsRecall: false })
        {
            return;
        }
        
        player.IsRecall = true;
        player.RecallTime = DateTimeOffset.Now.AddSeconds(_zoneData.MatchCard.RecallDuration);
        _unbufferedZone.SendDoStartRecall(playerUnitId, _zoneData.MatchCard.RecallDuration, (ulong)player.RecallTime.Value.ToUnixTimeMilliseconds());  
    }

    public void ReceivedSurrenderRequest(ushort rpcId, uint playerId, IServiceZone surrenderService)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            surrenderService.SendSurrenderStart(rpcId, SurrenderStartResultType.Disabled);
            return;
        }

        if (_zoneData.Phase.PhaseType is not (ZonePhaseType.Assault or ZonePhaseType.Assault2
            or ZonePhaseType.SuddenDeath) || (_attackStartTime.HasValue &&
                                              _attackStartTime.Value.AddSeconds(
                                                  _zoneData.MatchCard.SurrenderLogic?.TimeBeforeSurrender ?? 0) >
                                              DateTimeOffset.Now))
        {
            surrenderService.SendSurrenderStart(rpcId, SurrenderStartResultType.TooEarly);
        }

        if (_lastSurrenderTime[(int)player.Team] is { } surrTime &&
            surrTime.AddSeconds(_zoneData.MatchCard.SurrenderLogic?.TimeBetweenVoting ?? 0) > DateTimeOffset.Now)
        {
            surrenderService.SendSurrenderStart(rpcId, SurrenderStartResultType.TooFrequent);
        }

        if (_zoneData.IsSurrenderRequest[(int)player.Team])
        {
            surrenderService.SendSurrenderStart(rpcId, SurrenderStartResultType.InProgress);
        }

        _zoneData.IsSurrenderRequest[(int)player.Team] = true;
        var endTime = DateTimeOffset.Now
            .AddSeconds(_zoneData.MatchCard.SurrenderLogic?.VotingTime ?? 30f);
        _zoneData.SurrenderEndTime[(int)player.Team] = endTime;
        foreach (var vote in _zoneData.SurrenderVotes.Keys.ToList())
        {
            if (!_playerIdToUnitId.TryGetValue(playerId, out var pUnitId) ||
                !_playerUnits.TryGetValue(playerUnitId, out var p) || 
                p.Team == player.Team)
            {
                _zoneData.SurrenderVotes[vote] = null;
            }
        }
        
        _serviceZone.SendSurrenderProgress(_zoneData.SurrenderVotes);
        surrenderService.SendSurrenderStart(rpcId, SurrenderStartResultType.Accepted);
        var surrenderEnd = (ulong)endTime.ToUnixTimeMilliseconds();
        
        foreach (var teammate in _playerUnits.Values.Where(u => u.Team == player.Team))
        {
            teammate.ZoneService?.SendSurrenderBegin(surrenderEnd);
        }
        
        ReceivedSurrenderVoteRequest(playerId, true);
    }

    public void ReceivedSurrenderVoteRequest(uint playerId, bool accept)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player) ||
            !_zoneData.IsSurrenderRequest[(int)player.Team])
        {
            return;
        }

        _zoneData.SurrenderVotes[playerId] = accept;
        _serviceZone.SendSurrenderProgress(_zoneData.SurrenderVotes);
        if (_zoneData.MatchCard.SurrenderLogic?.MinVotes is null) return;
        
        var yesCount = 0;
        var noCount = 0;

        foreach (var play in _playerUnits.Values.Where(p => p.Team == player.Team && p.PlayerId.HasValue))
        {
            if (_zoneData.SurrenderVotes.TryGetValue(play.PlayerId.Value, out var vote))
            {
                switch (vote)
                {
                    case true:
                        yesCount++;
                        break;
                    case false:
                        noCount++;
                        break;
                }
            }
        }
        
        var maxLogic = _zoneData.MatchCard.SurrenderLogic.MinVotes.Max(k => k.Key);
        var playerCount = _playerUnits.Values.Count(p => p.Team == player.Team);
        var voteCount = _zoneData.MatchCard.SurrenderLogic.MinVotes[maxLogic];
        if (_zoneData.MatchCard.SurrenderLogic.MinVotes.TryGetValue(playerCount, out var actVoteCount))
        {
            voteCount = actVoteCount;
        }
        
        if (yesCount >= voteCount)
        {
            _serviceZone.SendSurrenderEnd(player.Team, true);
            _zoneData.IsSurrenderRequest[(int)player.Team] = false;
            _zoneData.SurrenderEndTime[(int)player.Team] = null;
            _endMatchTask = BeginEndOfGame(player.Team switch
            {
                TeamType.Neutral => TeamType.Team1,
                TeamType.Team1 => TeamType.Team2,
                TeamType.Team2 => TeamType.Team1,
                _ => TeamType.Neutral
            });
            
            _units.Values.Where(u => u.Team == player.Team && u.UnitCard?.IsBase is true).ToList().ForEach(DropUnit);
        }
        else if (noCount > playerCount - voteCount)
        {
            _serviceZone.SendSurrenderEnd(player.Team, false);
            _zoneData.IsSurrenderRequest[(int)player.Team] = false;
            _zoneData.SurrenderEndTime[(int)player.Team] = null;
            _lastSurrenderTime[(int)player.Team] = DateTimeOffset.Now;
        }
    }

    public void ReceivedEditorCommand(uint playerId, MapEditorCommand command, bool force)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player) ||
            (!_gameInitiator.IsMapEditor() && !force))
        {
            return;
        }

        switch (command)
        {
            case MapEditorCommand.Respawn:
                if (!player.IsDead)
                {
                    player.Killed(player.CreateBlankImpactData());
                }
                player.RespawnTime = DateTimeOffset.Now.AddSeconds(0.5);
                break;
            case MapEditorCommand.KillPlayer:
                player.Killed(player.CreateBlankImpactData());
                break;
            case MapEditorCommand.SkipBuildPhase:
                if (_zoneData.Phase.PhaseType is ZonePhaseType.Build or ZonePhaseType.Build2)
                {
                    UpdatePhase();
                }
                break;
            case MapEditorCommand.SpawnSupplyDrop:
                var supplyPosition = GetSupplyPosition(new SupplySequenceItem
                {
                    Seconds = 0,
                    SupplyUnitKey = CatalogueHelper.SupplyDrop,
                    DropPointLabel = UnitLabel.DropPointResource
                });

                if (supplyPosition is not null)
                {
                    CreateSupplyUnit(CatalogueHelper.SupplyDrop, supplyPosition.Value);
                }
                break;
            case MapEditorCommand.SpawnBlockbuster:
                var bbPosition = GetSupplyPosition(new SupplySequenceItem
                {
                    Seconds = 0,
                    SupplyUnitKey = CatalogueHelper.ClassicBlockbuster,
                    DropPointLabel = UnitLabel.DropPointBlockbuster
                });

                if (bbPosition is not null)
                {
                    CreateSupplyUnit(CatalogueHelper.ClassicBlockbuster, bbPosition.Value);
                }
                break;
            case MapEditorCommand.ResetCooldowns:
                if (player.TimeTillNextAbilityCharge is not null)
                {
                    player.TimeTillNextAbilityCharge = DateTimeOffset.Now;
                    player.UpdateData(new UnitUpdate
                    {
                        AbilityChargeCooldownEnd = 0
                    });
                }
                break;
            case MapEditorCommand.WinMatch:
                _endMatchTask = BeginEndOfGame(player.Team, false);
                break;
        }
    }
}