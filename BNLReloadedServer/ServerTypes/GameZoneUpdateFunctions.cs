using System.Numerics;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.Octree_Extensions;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.Service;
using MatchType = BNLReloadedServer.BaseTypes.MatchType;

namespace BNLReloadedServer.ServerTypes;

public partial class GameZone
{
    private void UnitCreated(Unit unit, UnitInit unitInit, IServiceZone? creatorService = null)
    {
        if (_units.TryAdd(unit.Id, unit))
        {
            if (unitInit.PlayerId != null)
            {
                _playerUnits.Add(unit.Id, unit);
                _playerIdToUnitId.Add(unitInit.PlayerId.Value, unit.Id);
            }
        }

        if (unitInit.Transform is not null)
        {
            AddUnitToOctree(unit, unitInit.Transform);
        }

        if (unit.UnitCard?.Labels?.Contains(UnitLabel.RespawnPoint) is true)
        {
            unit.SpawnId = NewSpawnId();
            UpdateSpawnPoint(unit);
            _playerSpawnPoints.Add(unit.SpawnId.Value, unit);
        }

        if (_sessionsSender.SenderCount == 0 && unit.PlayerId == null) return;
        if (unitInit.Controlled)
        {
            _serviceZone.SendUnitCreate(unit.Id, unitInit);
        }
        else
        {
            _unbufferedZone.SendUnitCreate(unit.Id, unitInit);
            if (_gameLoop != null && (unit.IsDropped || unit.PlayerId == null))
            {
                creatorService?.SendUnitControl(unit.Id);
            }
            else
            {
                unitInit.Controlled = true;
                if (_gameLoop is null && unit.PlayerId != null)
                {
                    _createOnStart.Add((unit.Id, unitInit, () => unit.ZoneService));
                }
                else
                {
                    creatorService?.SendUnitCreate(unit.Id, unitInit);
                }
            }
        }
        
        if (unitInit.Transform is not null && _gameLoop != null)
        {
            RunBlockCheckForUnit(unit);
        }
    }

    private void UnitUpdated(Unit unit, UnitUpdate unitUpdate, bool unbuffered = false)
    {
        if (_sessionsSender.SenderCount == 0 || unit.IsDropped) return;
        if (_gameLoop == null || unbuffered)
        {
            _unbufferedZone.SendUnitUpdate(unit.Id, unitUpdate);
        }
        else
        {
            _serviceZone.SendUnitUpdate(unit.Id, unitUpdate);
        }
    }

    private void UnitMoved(Unit unit, ulong time, ZoneTransform transform, Vector3 oldPosition)
    {
        if ((unit.UnitCard?.AllowUnderwater is not false || !(oldPosition.Y >= _zoneData.PlanePosition &&
                                                             transform.Position.Y < _zoneData.PlanePosition)) && 
            (_zoneData.MapData.Properties?.KillPosition is null ||
             !(oldPosition.Y >= _zoneData.MapData.Properties.KillPosition &&
               transform.Position.Y < _zoneData.MapData.Properties.KillPosition)))
        {
            _unitOctree.Remove(unit);
            AddUnitToOctree(unit, transform);
            _serviceZone.SendUnitMove(unit.Id, time, transform);
            if (unit.IsRecall && (oldPosition - transform.Position).LengthSquared() > 9.99999944E-11f)
            {
                unit.EndRecall();
                _serviceZone.SendDoCancelRecall(unit.Id);
            }
            RunBlockCheckForUnit(unit);
        }
        else
        {
            var sourceKey = _zoneData.MapData.Properties?.Plane == "LavaPlane"
                ? CatalogueHelper.LavaSource
                : CatalogueHelper.AcidSource;
            var impactData = unit.CreateBlankImpactData();
            impactData.SourceKey = sourceKey;
            unit.Killed(impactData);
        }
    }

    private void UnitTeamEffectAdded(Unit unit, ConstEffectInfo effectInfo)
    {
        IEnumerable<Unit> teamUnits;
        if (effectInfo.Card.Effect is not ConstEffectTeam constEffect) return;
        var effects = constEffect.ConstantEffects?.Select(e => new ConstEffectInfo(e)).ToList();
        if (effects is null or { Count: <= 0 }) return;
        
        switch (constEffect.Targeting)
        {
            case { AffectedTeam: RelativeTeamType.Friendly }:
                _teamEffects[(int)unit.Team].Add(effectInfo);
                teamUnits = _units.Values.Where(u => u.Team == unit.Team);
                break;
            case { AffectedTeam: RelativeTeamType.Opponent }:
                foreach (var element in Enum.GetValues<TeamType>().Where(t => t != unit.Team))
                {
                    _teamEffects[(int)element].Add(effectInfo);
                }
                teamUnits = _units.Values.Where(u => u.Team != unit.Team);    
                break;
            default:
                foreach (var element in _teamEffects)
                {
                    element.Add(effectInfo);
                }

                teamUnits = _units.Values;
                break;
        }

        if (constEffect.Targeting?.IgnoreCaster ?? false)
        {
            teamUnits = teamUnits.Except([unit]);
        }
        
        foreach (var element in teamUnits)
        {
            element.AddEffects(effects, unit.Team, unit.GetSelfSource());
        }
    }

    private void UnitTeamEffectRemoved(Unit unit, ConstEffectInfo effectInfo)
    {
        IEnumerable<Unit> teamUnits;
        if (effectInfo.Card.Effect is not ConstEffectTeam constEffect) return;
        var effects = constEffect.ConstantEffects?.Select(e => new ConstEffectInfo(e, null)).ToList();
        if (effects is null or { Count: <= 0 }) return;
        
        switch (constEffect.Targeting)
        {
            case { AffectedTeam: RelativeTeamType.Friendly }:
                _teamEffects[(int)unit.Team].Remove(effectInfo);
                teamUnits = _units.Values.Where(u => u.Team == unit.Team);
                break;
            case { AffectedTeam: RelativeTeamType.Opponent }:
                foreach (var element in Enum.GetValues<TeamType>().Where(t => t != unit.Team))
                {
                    _teamEffects[(int)element].Remove(effectInfo);
                }
                teamUnits = _units.Values.Where(u => u.Team != unit.Team);    
                break;
            default:
                foreach (var element in _teamEffects)
                {
                    element.Remove(effectInfo);
                }

                teamUnits = _units.Values;
                break;
        }

        if (constEffect.Targeting?.IgnoreCaster ?? false)
        {
            teamUnits = teamUnits.Except([unit]);
        }

        foreach (var element in teamUnits)
        {
            element.RemoveEffects(effects, unit.Team, unit.GetSelfSource());
        }
    }

    private const float ImpactImprecision = 0.05f;
    private const float ImpactImprecisionInv = 1 - ImpactImprecision;
    
    private bool ApplyInstEffect(EffectSource source, IEnumerable<Unit> affectedUnits, InstEffect effect,
        ImpactData impactData, BlockShift? shift = null, Direction2D? sourceDirection = null, ResourceType? resourceType = null, 
        bool damageBlock = true)
    {
        var unitSource = source switch
        {
            BlockSource blockSrc => MapBinary.OwnedBlocks.GetValueOrDefault(blockSrc.Position),
            UnitSource unitSrc => unitSrc.Unit,
            _ => null,
        };
        
        var impactPoint = impactData.InsidePoint;
        if (impactData.Normal != Vector3s.Zero)
        {
            impactPoint = impactData.InsidePoint + ZoneTransformHelper.ToVector3(impactData.Normal);
        }

        var actualUnits = effect switch
        {
            InstEffectAllUnitsBunch allUnitsBunch => allUnitsBunch.Range is not null
                ? _unitOctree.GetColliding(new BoundingSphere(impactPoint, allUnitsBunch.Range.Value))
                : _units.Values.ToList(),
            
            InstEffectChargeTesla => affectedUnits.Where(u => u is
            {
                TeslaUnitData: not null, TeslaCharge: not TeslaChargeType.FullSelfCharge
            }),
            
            InstEffectDamageBlocks { Damage.PlayerDamage: > 0 } instEffectDamageBlocks => _unitOctree.GetColliding(
                new BoundingSphere(impactPoint, instEffectDamageBlocks.Range)),
            
            InstEffectFireMortars instEffectFireMortars when (instEffectFireMortars.OwnedMortarsOnly
                    ? _units.Values.Where(u => u.UnitCard?.Data is UnitDataMortar && unitSource is not null &&
                                               u.OwnerPlayerId == unitSource.OwnerPlayerId)
                    : _units.Values.Where(u => u.UnitCard?.Data is UnitDataMortar && u.Team == source.Team))
                is { } applicableMortars => 
                instEffectFireMortars.MaxMortars is not null
                    ? applicableMortars.OrderBy(m => Vector3.DistanceSquared(m.Transform.Position, impactPoint))
                        .Take(instEffectFireMortars.MaxMortars.Value)
                    : applicableMortars,
            
            InstEffectKnockback instEffectKnockback => _unitOctree
                .GetColliding(new BoundingSphere(impactPoint, instEffectKnockback.EffectRange))
                .Where(u => u.PlayerId != null && !u.IsBuff(BuffType.KnockbackIgnore) && !u.IsBuff(BuffType.Root)),
            
            InstEffectResourceAll => _playerUnits.Values.ToList(),
            
            InstEffectSplashDamage instEffectSplashDamage => _unitOctree.GetColliding(
                new BoundingSphere(impactPoint, instEffectSplashDamage.Radius)),
            
            InstEffectZoneEffect => _playerUnits.Values.ToList(),
            
            _ => affectedUnits
        };

        if (effect.Targeting?.CasterOwnedOnly is true)
        {
            actualUnits = actualUnits.Where(u => unitSource is not null &&
                u.OwnerPlayerId == unitSource.PlayerId && u.OwnerPlayerId.HasValue == unitSource.PlayerId.HasValue);
        }

        if (effect.Targeting?.IgnoreCaster is true && unitSource is not null)
        {
            actualUnits = actualUnits.Except([unitSource]);
        }

        if (effect.Targeting?.AffectedLabels is { Count: > 0 } labels)
        {
            actualUnits = actualUnits.Where(u => u.UnitCard?.Labels?.Intersect(labels).Any() is true);
        }

        actualUnits = effect.Targeting switch
        {
            { AffectedTeam: RelativeTeamType.Friendly } => actualUnits.Where(u => u.Team == source.Team),
            { AffectedTeam: RelativeTeamType.Opponent } => actualUnits.Where(u => u.Team != source.Team),
            _ => actualUnits
        };

        if (effect.Targeting?.AffectedUnits is { Count: > 0 } inclUnits)
        {
            actualUnits = actualUnits.Where(u => u.UnitCard?.Data?.Type is { } uType && inclUnits.Contains(uType));
        } 
        
        var actualUnitList = actualUnits.ToList();

        if (effect.Interrupt?.Recall is true)
        {
            switch (effect)
            {
                case InstEffectAddAmmo:
                case InstEffectAddAmmoPercent:
                case InstEffectAddResource:
                case InstEffectAllPlayersPersistent:
                case InstEffectAllUnitsBunch:
                case InstEffectBunch:
                case InstEffectDamage:
                case InstEffectDrainAmmo:
                case InstEffectDrainMagazineAmmo:
                case InstEffectHeal:
                case InstEffectInstReload:
                case InstEffectKill:
                case InstEffectKnockback:
                case InstEffectPurge:
                case InstEffectResourceAll:
                case InstEffectSlip:
                case InstEffectSupply:
                case InstEffectTeleport:
                case InstEffectTeleportTo:
                case InstEffectZoneEffect:
                    foreach (var unit in actualUnitList.Where(u => u.IsRecall))
                    {
                        unit.EndRecall();
                        _serviceZone.SendDoCancelRecall(unit.Id);
                    }
                    break;
                
                case InstEffectCasterBunch when unitSource?.IsRecall is true:
                    unitSource.EndRecall();
                    _serviceZone.SendDoCancelRecall(unitSource.Id);
                    break;
            }
        }
        
        var hasImpact = false;
        if (effect.Impact is { } imp && Databases.Catalogue.GetCard<CardImpact>(imp) is { } impact)
        {
            hasImpact = true;
            impactData.Impact = impact.Key;
            switch (effect)
            {
                case InstEffectAddAmmo:
                case InstEffectAddAmmoPercent:
                case InstEffectAddResource:
                case InstEffectBuildDevice:
                case InstEffectDamage:
                case InstEffectDrainAmmo:
                case InstEffectDrainMagazineAmmo:
                case InstEffectHeal:
                case InstEffectInstReload:
                case InstEffectKill:
                case InstEffectPurge:
                case InstEffectSlip:
                case InstEffectSupply:
                case InstEffectTeleport:
                case InstEffectTeleportTo:
                case InstEffectChargeTesla:
                case InstEffectFireMortars:   
                case InstEffectZoneEffect:
                    impactData.HitUnits = actualUnitList.Select(u => u.Id).ToList();
                    ImpactOccur(impactData);
                    break;
                
                case InstEffectBlocksSpawn:
                case InstEffectDamageBlocks:
                case InstEffectHealBlocks:
                case InstEffectReplaceBlocks:
                case InstEffectUnitSpawn:
                    impactData.HitUnits = [];
                    ImpactOccur(impactData);
                    break;
                
                case InstEffectAllUnitsBunch:
                case InstEffectBunch:    
                    impactData.HitUnits = actualUnitList.Select(u => u.Id).ToList();
                    break;
                
                case InstEffectCasterBunch when unitSource is not null:
                    impactData.HitUnits = [unitSource.Id];
                    break;
            }
        }

        switch (effect)
        {
            case InstEffectAddAmmo instEffectAddAmmo:
                actualUnitList.ForEach(u => u.AddAmmo(instEffectAddAmmo.Amount));
                return true;
            
            case InstEffectAddAmmoPercent instEffectAddAmmoPercent:
                actualUnitList.ForEach(u => u.AddAmmoPercent(instEffectAddAmmoPercent.Fraction));
                return true;
            
            case InstEffectAddResource instEffectAddResource when unitSource is not null:
                ResourceType resType;
                if (resourceType.HasValue)
                {
                    resType = resourceType.Value;
                }
                else if (instEffectAddResource.Supply)
                {
                    resType = ResourceType.Supply;
                }
                else
                {
                    resType = ResourceType.General;
                }

                var amount = instEffectAddResource.Amount;
                if (MathF.Abs(amount) < 1)
                {
                    amount *= unitSource.Resource;
                }

                if (amount < 0)
                {
                    amount = MathF.Abs(amount);
                    unitSource.RemoveResources(amount);
                }
                else
                {
                    unitSource.AddResource(amount, resType);
                }
                
                return true;
            
            case InstEffectAllPlayersPersistent instEffectAllPlayersPersistent:
                var playerEnumer = instEffectAllPlayersPersistent switch
                {
                    { AffectedTeam: RelativeTeamType.Friendly } => _playerUnits.Values.Where(p => p.Team == source.Team),
                    { AffectedTeam: RelativeTeamType.Opponent } => _playerUnits.Values.Where(p => p.Team != source.Team),
                    _ => _playerUnits.Values
                };

                if (!instEffectAllPlayersPersistent.IncludeDeadPlayers)
                {
                    playerEnumer = playerEnumer.Where(p => !p.IsDead);
                }

                var players = playerEnumer.ToList();

                if (hasImpact)
                {
                    impactData.HitUnits = players.Select(p => p.Id).ToList();
                    ImpactOccur(impactData);
                }

                var constEffects = instEffectAllPlayersPersistent.Constant?.Select(c =>
                    new ConstEffectInfo(c, instEffectAllPlayersPersistent.PersistenceDuration));
                if (constEffects != null)
                {
                    players.ForEach(plyer => plyer.AddEffects(constEffects, source.Team, source));
                }
                return true;
            
            case InstEffectAllUnitsBunch instEffectAllUnitsBunch:
                if (instEffectAllUnitsBunch.Constant is { Count: > 0 } constant)
                {
                   foreach (var unit in actualUnitList)
                   {
                       unit.AddEffects(constant.Select(c => new ConstEffectInfo(c)), source.Team, source);
                   } 
                }

                var successAll = true;
                var beforeAll = impactData.Clone();
                
                if (instEffectAllUnitsBunch.Instant is { Count: > 0 } instant)
                {
                    successAll = !instant.Any(ins =>
                        !ApplyInstEffect(source, actualUnitList, ins, impactData, shift, sourceDirection,
                            resourceType) && instEffectAllUnitsBunch.BreakOnEffectFail);
                }

                if (successAll && hasImpact)
                {
                    ImpactOccur(beforeAll);
                }

                return successAll;

            case InstEffectBlocksSpawn { Pattern: not null } instEffectBlocksSpawn:
                var addUpdates =
                    _zoneData.BlocksData.AddBlocks(instEffectBlocksSpawn.Pattern, impactPoint, shift, unitSource);
                if (addUpdates.Count > 0)
                {
                    DoBlockUpdate(addUpdates); 
                }
                return true;
            
            case InstEffectBuildDevice instEffectBuildDevice when unitSource is not null:
                if (unitSource.Resource < instEffectBuildDevice.TotalCost) return false;
                var blockLoc = (Vector3s)(impactData.InsidePoint + shift switch
                {
                    BlockShift.Left => Vector3s.Left.ToVector3(),
                    BlockShift.Right => Vector3.UnitX,
                    BlockShift.Bottom => Vector3s.Down.ToVector3(),
                    BlockShift.Top => Vector3.UnitY,
                    BlockShift.Back => Vector3s.Back.ToVector3(),
                    BlockShift.Front => Vector3.UnitZ,
                    _ => Vector3.Zero
                });
                if (!_zoneData.BlocksData.ContainsBlock(blockLoc)) return false;
                
                var devCard = Databases.Catalogue.GetCard<CardDevice>(instEffectBuildDevice.DeviceKey);
                var itemCard = devCard?.DeviceKeyAtLevel((byte)instEffectBuildDevice.Level);
                if (devCard is null || itemCard is null) return false;
                switch (Databases.Catalogue.GetCard(itemCard.Value))
                {
                    case CardBlock blockCard: 
                        var updates = _zoneData.BlocksData.AddBlock(blockCard.Key, blockLoc, (Vector3s)impactData.InsidePoint,
                            unitSource.CurrentBuildInfo?.Direction ?? Direction2D.Left, unitSource);
                        if (updates.Count <= 0) return true;
                        
                        DoBlockUpdate(updates);
                        unitSource.RemoveResources(instEffectBuildDevice.TotalCost);
                        if (unitSource.PlayerId is not null)
                        {
                            unitSource.BuiltBlock(blockCard.DeviceType, instEffectBuildDevice.TotalCost);
                            _serviceZone.SendDeviceBuilt(unitSource.PlayerId.Value, devCard.Key, blockLoc.ToVector3() + new Vector3(0.5f));
                        }
                        return true;
                    
                    case CardUnit unitCard:
                        if (unitCard.CountLimit is not null)
                        {
                            var existingUnitCount = unitCard.CountLimit.Scope switch
                            {
                                UnitLimitScope.World => _units.Values.Count(u => u.Key == unitCard.Key),
                
                                UnitLimitScope.Team => _units.Values.Count(u =>
                                    u.Key == unitCard.Key && u.Team == source.Team),
                
                                UnitLimitScope.Owner => _units.Values
                                    .Count(u => u.Key == unitCard.Key && u.OwnerPlayerId == unitSource.OwnerPlayerId),
                
                                _ => 0
                            };

                            if (existingUnitCount > unitCard.CountLimit.Limit) return false;
                        }
                        
                        var placePos = blockLoc.ToVector3() + new Vector3(0.5f);
                        var placeDirection = devCard.InverseDirection ? (sourceDirection ?? Direction2D.Left).Inverse() : sourceDirection ?? Direction2D.Left;
                        var rotation = BuildHelper.GetBuildRotation(devCard, blockLoc, (Vector3s)impactData.InsidePoint,
                            placeDirection);
                        var transform = ZoneTransformHelper.ToZoneTransform(placePos, rotation);
                        transform.LocalVelocity = Vector3s.Zero;
                        var collision = false;
                        
                        var checkPosition = BuildHelper.GetAttachmentType(blockLoc,
                                (Vector3s)impactData.InsidePoint) switch
                            {
                                BuildHelper.BluidAttachmentType.Floor when devCard.AttachFloor =>
                                    blockLoc with { y = (short)(blockLoc.y - 1) },

                                BuildHelper.BluidAttachmentType.Floor when devCard.AttachCeiling =>
                                    blockLoc with { y = (short)(blockLoc.y + 1) },

                                BuildHelper.BluidAttachmentType.Floor =>
                                    blockLoc with { y = (short)(blockLoc.y - 1) },

                                BuildHelper.BluidAttachmentType.Ceiling when devCard.AttachCeiling =>
                                    blockLoc with { y = (short)(blockLoc.y + 1) },

                                BuildHelper.BluidAttachmentType.Ceiling =>
                                    blockLoc with { y = (short)(blockLoc.y - 1) },

                                BuildHelper.BluidAttachmentType.Walls when devCard.AttachWalls =>
                                    (Vector3s)impactData.InsidePoint,

                                BuildHelper.BluidAttachmentType.Walls when devCard.AttachFloor =>
                                    blockLoc with { y = (short)(blockLoc.y - 1) },

                                BuildHelper.BluidAttachmentType.Walls when devCard.AttachCeiling =>
                                    blockLoc with { y = (short)(blockLoc.y + 1) },

                                _ => (Vector3s)impactData.InsidePoint
                            };
                        
                        if (unitCard.Size is not null && unitCard.Size != Vector3s.Zero)
                        {
                            if (MapBinary.ContainsBlock(checkPosition) && MapBinary[checkPosition].Card.IsVisualSlope &&
                                MapBinary[checkPosition].VData != 0 &&
                                UnitSizeHelper.IsInsideUnit(checkPosition, unitSource) || _unitOctree.GetColliding(
                                    new BoundingBoxEx(
                                        checkPosition.ToVector3() + unitCard.Size.Value.ToVector3() * 0.5f,
                                        unitCard.Size.Value.ToVector3() - UnitSizeHelper.ImprecisionVector))
                                    .Where(u => u.PlayerId != null).Except([unitSource]).Any()) 
                                return false;
                            
                            collision = UnitSizeHelper.IsInsideUnit(blockLoc, unitSource) || _unitOctree.GetColliding(
                                new BoundingBoxEx(
                                blockLoc.ToVector3() + unitCard.Size.Value.ToVector3() * 0.5f,
                                unitCard.Size.Value.ToVector3() - UnitSizeHelper.ImprecisionVector)).Except([unitSource]).Any();
                        }

                        var isAttached = !unitCard.GroundOnly ||
                                         BuildHelper.GetAttachmentType(blockLoc,
                                                 (Vector3s)impactData.InsidePoint) switch
                                             {
                                                 BuildHelper.BluidAttachmentType.Floor when devCard.AttachFloor =>
                                                     blockLoc.y > 0 && _zoneData
                                                         .BlocksData.GetValidFaces(blockLoc with
                                                         {
                                                             y = (short)(blockLoc.y - 1)
                                                         }, true).Contains(blockLoc),

                                                 BuildHelper.BluidAttachmentType.Floor when devCard.AttachCeiling =>
                                                     blockLoc.y < _zoneData.BlocksData.SizeY &&
                                                     _zoneData
                                                         .BlocksData.GetValidFaces(blockLoc with
                                                         {
                                                             y = (short)(blockLoc.y + 1)
                                                         }, true).Contains(blockLoc),

                                                 BuildHelper.BluidAttachmentType.Floor =>
                                                     blockLoc.y > 0 && _zoneData
                                                         .BlocksData.GetValidFaces(blockLoc with
                                                         {
                                                             y = (short)(blockLoc.y - 1)
                                                         }, true).Contains(blockLoc),

                                                 BuildHelper.BluidAttachmentType.Ceiling when devCard.AttachCeiling =>
                                                     blockLoc.y < _zoneData.BlocksData.SizeY &&
                                                     _zoneData
                                                         .BlocksData.GetValidFaces(blockLoc with
                                                         {
                                                             y = (short)(blockLoc.y + 1)
                                                         }, true).Contains(blockLoc),

                                                 BuildHelper.BluidAttachmentType.Ceiling =>
                                                     blockLoc.y > 0 && _zoneData
                                                         .BlocksData.GetValidFaces(blockLoc with
                                                         {
                                                             y = (short)(blockLoc.y - 1)
                                                         }, true).Contains(blockLoc),

                                                 BuildHelper.BluidAttachmentType.Walls when devCard.AttachWalls =>
                                                     _zoneData.BlocksData.ContainsBlock(
                                                         (Vector3s)impactData.InsidePoint) &&
                                                     _zoneData
                                                         .BlocksData.GetValidFaces((Vector3s)impactData.InsidePoint, true)
                                                         .Contains(blockLoc),

                                                 BuildHelper.BluidAttachmentType.Walls when devCard.AttachFloor =>
                                                     blockLoc.y > 0 && _zoneData
                                                         .BlocksData.GetValidFaces(blockLoc with
                                                         {
                                                             y = (short)(blockLoc.y - 1)
                                                         }, true).Contains(blockLoc),

                                                 BuildHelper.BluidAttachmentType.Walls when devCard.AttachCeiling =>
                                                     blockLoc.y < _zoneData.BlocksData.SizeY &&
                                                     _zoneData
                                                         .BlocksData.GetValidFaces(blockLoc with
                                                         {
                                                             y = (short)(blockLoc.y + 1)
                                                         }, true).Contains(blockLoc),

                                                 _ => true
                                             };

                        if (!_zoneData.BlocksData[blockLoc].IsReplaceable || !isAttached ||
                            (!unitCard.AllowUnderwater && blockLoc.y <= _zoneData.PlanePosition) || collision)
                            return false;
                        var isAttachedToBlock = isAttached && unitCard.BlockBinding is UnitBlockBindingType.Destroy
                                                    or UnitBlockBindingType.Detach;

                        
                        var newUnit = CreateUnit(unitCard, transform, unitSource, unitSource.ZoneService, isAttachedToBlock);
                        if (newUnit is null)
                        {
                            return false;
                        }

                        if (unitSource.Devices.Values.FirstOrDefault(d => d.DeviceKey == devCard.Key) is { } devData &&
                            unitCard.CostIncPerUnit is { } baseCostInc and > 0)
                        {
                            var costInc = baseCostInc;
                            devData.TotalCost += costInc;
                            devData.CostInc += costInc;
                            unitSource.UpdateData(new UnitUpdate
                            {
                                Devices = unitSource.Devices
                            });

                            newUnit.OnDestroyed = () =>
                            {
                                if (unitSource.Devices.Values.FirstOrDefault(d => d.DeviceKey == devCard.Key) is
                                    not { CostInc: > 0 } dData) return;
                                
                                dData.TotalCost -= costInc;
                                dData.CostInc -= costInc;
                                
                                unitSource.UpdateData(new UnitUpdate
                                {
                                    Devices = unitSource.Devices
                                });
                            };
                        }
                        
                        var blkUpdates = _zoneData.BlocksData.RemoveBlock(blockLoc);
                        var blkUpdates2 =
                            _zoneData.BlocksData.MakeSlopeSolid(blockLoc, checkPosition);
                        foreach (var upd in blkUpdates2)
                        {
                            blkUpdates[upd.Key] = upd.Value;
                        }
                        unitSource.RemoveResources(instEffectBuildDevice.TotalCost);
                        if (blkUpdates.Count > 0)
                        {
                            DoBlockUpdate(blkUpdates);
                        }
                        
                        if (isAttachedToBlock)
                        {
                            if (!MapBinary.AttachToBlock(newUnit, checkPosition,
                                    CoordsHelper.VectorToFace(blockLoc - checkPosition)))
                            {
                                OnDetached(newUnit);
                            }
                        }

                        if (unitSource.PlayerId is not null)
                        {
                            unitSource.BuiltBlock(unitCard.DeviceType, instEffectBuildDevice.TotalCost);
                            _serviceZone.SendDeviceBuilt(unitSource.PlayerId.Value, devCard.Key, placePos);
                        }
                            
                        return true;
                    default:
                        return false;
                }
                
            case InstEffectBunch instEffectBunch:
                if (instEffectBunch.Constant is { Count: > 0 } con)
                {
                    foreach (var unit in actualUnitList)
                    {
                        unit.AddEffects(con.Select(c => new ConstEffectInfo(c)), source.Team, source);
                    } 
                }
                
                var before = impactData.Clone();
                var success = true;
                
                if (instEffectBunch.Instant is { Count: > 0 } inst)
                {
                    success = !inst.Any(ins =>
                        !ApplyInstEffect(source, actualUnitList, ins, impactData, shift, sourceDirection,
                            resourceType) && instEffectBunch.BreakOnEffectFail);
                }

                if (success && hasImpact)
                {
                    ImpactOccur(before);
                }

                return success;
            
            case InstEffectCasterBunch instEffectCasterBunch when unitSource is not null:
                if (instEffectCasterBunch.Constant is { Count: > 0 } cons)
                {
                    unitSource.AddEffects(cons.Select(c => new ConstEffectInfo(c)), source.Team, source);
                }
                
                var beforeCast = impactData.Clone();
                var successCast = true;
                
                if (instEffectCasterBunch.Instant is { Count: > 0 } insta)
                {
                    successCast = !insta.Any(ins =>
                        !ApplyInstEffect(source, [unitSource], ins, impactData, shift, sourceDirection, resourceType) &&
                        instEffectCasterBunch.BreakOnEffectFail);
                }
                
                if (successCast && hasImpact)
                {
                    ImpactOccur(beforeCast);
                }

                return successCast;
            
            case InstEffectChargeTesla instEffectChargeTesla:
                foreach (var tesla in actualUnitList)
                {
                    tesla.ChargeTesla(instEffectChargeTesla.Charges);
                    var (props, target, tes) = PropagateTesla(tesla, [], []);
                    if (target is not null && props is not null)
                    {
                        ReceivedUpdateTesla(tes, target, props);
                    }
                }
                return true;
            
            case InstEffectDamage { Damage: not null } instEffectDamage:
                var dData = ConvertToDamageData(instEffectDamage.Damage, impactPoint, impactData.ShotPos,
                    unitSource, false, impactData.Crit, instEffectDamage.CritModifier, instEffectDamage.Falloff);

                if (source is BlockSource { Block.Id: 30 })
                {
                    dData = dData with { SelfDamage = dData.EnemyDamage, FriendDamage = dData.EnemyDamage };
                }
                
                foreach (var unit in actualUnitList)
                {
                    unit.TakeDamage(dData, impactData, false, unitSource, source.Team);
                }

                if (dData.BlockDamage > 0 && damageBlock && (Vector3s)impactPoint is var impPoint &&
                    MapBinary.ContainsBlock(impPoint))
                {
                    DoBlockUpdate(MapBinary.DamageBlock(impPoint, dData, unitSource));
                }
                
                return true;
            
            case InstEffectDamageBlocks { Damage: not null } instEffectDamageBlocks:
                var bDData = ConvertToDamageData(instEffectDamageBlocks.Damage, impactPoint,
                    impactData.ShotPos, unitSource);

                var blUpdates = new Dictionary<Vector3s, BlockUpdate>();
                foreach (var block in MapBinary.EnumerateBlocks(new BoundingSphere(impactPoint,
                             instEffectDamageBlocks.Range), null))
                {
                    foreach (var update in MapBinary.DamageBlock(block, bDData, unitSource, true))
                    {
                        blUpdates[update.Key] = update.Value;
                    }
                }

                if (instEffectDamageBlocks.Damage.PlayerDamage > 0)
                {
                    foreach (var unit in actualUnitList)
                    {
                        unit.TakeDamage(bDData, impactData, false, unitSource, source.Team);
                    }
                }

                if (blUpdates.Count > 0)
                {
                    DoBlockUpdate(blUpdates);
                }
                return true;
            
            case InstEffectDrainAmmo instEffectDrainAmmo:
                foreach (var unit in actualUnitList.Where(u => u.PlayerId != null))
                {
                    unit.TakeAmmo(instEffectDrainAmmo.Amount, false);
                }
                return true;
            
            case InstEffectDrainMagazineAmmo instEffectDrainMagazineAmmo:
                foreach (var unit in actualUnitList.Where(u => u.PlayerId != null))
                {
                    unit.TakeAmmo(instEffectDrainMagazineAmmo.Amount, true);
                }
                return true;
            
            case InstEffectFireMortars instEffectFireMortars:
                var rand = new Random();
                foreach (var (mortar, index) in actualUnitList.Select((item, index) => (item, index)))
                {
                    mortar.OnMortarHit = instEffectFireMortars.HitEffect;
                    var normalSpread = instEffectFireMortars.BaseSpread +
                                       index * instEffectFireMortars.IncrementalSpread;
                    var distSpread =
                        Math.Max(
                            Vector3.Distance(mortar.Transform.Position, impactPoint) *
                            instEffectFireMortars.DistanceSpread, instEffectFireMortars.MinSpreadPercent);

                    var totalSpread = normalSpread + distSpread;
                    var r = totalSpread * MathF.Sqrt(rand.NextSingle());
                    var theta = rand.NextSingle() * 2 * MathF.PI;
        
                    var newShotX = float.FusedMultiplyAdd(r, MathF.Cos(theta), impactPoint.X);
                    var newShotZ = float.FusedMultiplyAdd(r, MathF.Sin(theta), impactPoint.Z);
                    
                    var newShotPos = impactPoint with { X = newShotX, Z = newShotZ };

                    var delay = rand.NextSingle() * instEffectFireMortars.FireDelayModifier +
                                instEffectFireMortars.BaseFireDelay;

                    Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delay));
                        EnqueueAction(() =>
                        {
                            _serviceZone.SendFireMortar(mortar.Id, newShotPos);
                            if (instEffectFireMortars.MortarFireEffect is null) return;

                            var mortarImpact = mortar.CreateImpactData();
                            var mortarSource = mortar.GetSelfSource(mortarImpact);
                            ApplyInstEffect(mortarSource, [mortar], instEffectFireMortars.MortarFireEffect,
                                mortarImpact);
                        });
                    });
                }
                return true;
            
            case InstEffectHeal instEffectHeal:
                foreach (var unit in actualUnitList)
                {
                    float hpGain = 0;
                    if (unit.PlayerId != null)
                    {
                        hpGain = unit.AddHealth(instEffectHeal.PlayerHeal);
                        unit.AddForcefield(instEffectHeal.ForcefieldAmount);
                    }
                    else if (unit.UnitCard?.IsObjective ?? false)
                    {
                        hpGain = unit.AddHealth(instEffectHeal.ObjectiveHeal);
                        unit.AddForcefield(instEffectHeal.ForcefieldAmount);
                    }
                    else if (unit.IsHealth)
                    {
                        hpGain = unit.AddHealth(instEffectHeal.WorldHeal);
                        unit.AddForcefield(instEffectHeal.ForcefieldAmount);
                    }

                    if (hpGain > 0)
                    {
                        unit.Healed(hpGain, source,
                            impactData.CasterPlayerId.HasValue
                                ? GetPlayerFromPlayerId(impactData.CasterPlayerId.Value)
                                : null);
                    }
                }
                return true;
            
            case InstEffectHealBlocks instEffectHealBlocks:
                var boUpdates = new Dictionary<Vector3s, BlockUpdate>();
                foreach (var block in MapBinary.EnumerateBlocks(new BoundingSphere(impactPoint,
                             instEffectHealBlocks.Range), null))
                {
                    var playerSource = impactData.CasterPlayerId.HasValue 
                        ? GetPlayerFromPlayerId(impactData.CasterPlayerId.Value)
                        : null;
                    
                    foreach (var update in MapBinary.HealBlock(block, instEffectHealBlocks.HealAmount,
                                 out var healAmount))
                    {
                        boUpdates[update.Key] = update.Value;
                        
                        if (healAmount > 0)
                        {
                            playerSource?.RepairedBlock(healAmount, source);
                        }
                    }
                }

                if (boUpdates.Count > 0)
                {
                    DoBlockUpdate(boUpdates);
                }
                return true;
            
            case InstEffectInstReload instEffectInstReload:
                foreach (var unit in actualUnitList)
                {
                    unit.ReloadAmmo(instEffectInstReload.AllWeapons, instEffectInstReload.ClipPart);
                }
                return true;
            
            case InstEffectKill:
                foreach (var unit in actualUnitList.Where(u => u is { IsHealth: true, IsDead: false }))
                {
                    unit.Killed(impactData);
                }
                return true;
            
            case InstEffectKnockback instEffectKnockback:
                actualUnitList = actualUnitList.Where(u => u.PlayerId != null).ToList();
                
                if (!instEffectKnockback.AffectCaster && unitSource is not null)
                {
                    actualUnitList.RemoveAll(u => u.PlayerId == unitSource.OwnerPlayerId);
                }

                if (hasImpact)
                {
                    impactData.HitUnits = actualUnitList.Select(u => u.Id).ToList();
                    ImpactOccur(impactData);
                }

                foreach (var unit in actualUnitList)
                {
                    var unitPos = unit.GetExactPosition();
                    unitPos = Vector3.Clamp(impactPoint,
                        unitPos - new Vector3(0.25f, unit.Transform.IsCrouch ? 0.45f : 0.95f, 0.25f),
                        unitPos + new Vector3(0.25f, unit.Transform.IsCrouch ? 0.45f : 0.95f, 0.25f));
                    var distance = Vector3.Distance(impactPoint, unitPos);

                    var force = instEffectKnockback.Force;
                    var midairForce = instEffectKnockback.MidairForce;
                    if (instEffectKnockback is { LinearFalloff: true, EffectRange: > 0 })
                    {
                        var weight = distance / instEffectKnockback.EffectRange;
                        force = float.Lerp(instEffectKnockback.Force, 0.0f, weight);
                        midairForce = float.Lerp(instEffectKnockback.MidairForce, 0.0f, weight);
                    }

                    var knockback = new ManeuverKnockback
                    {
                        Origin = impactPoint,
                        Force = force,
                        MidairForce = midairForce
                    };
                    
                    _serviceZone.SendUnitManeuver(unit.Id, knockback);
                }
                return true;
            
            case InstEffectPurge instEffectPurge:
                actualUnitList.ForEach(u => u.PurgeEffects(instEffectPurge.Positive, instEffectPurge.Negative));
                return true;
            
            case InstEffectReplaceBlocks instEffectReplaceBlocks:
                var repUpdates = _zoneData.BlocksData.ReplaceBlocks(instEffectReplaceBlocks.ReplaceWith,
                    instEffectReplaceBlocks.Range, impactPoint, unitSource);
                if (repUpdates.Count > 0)
                {
                    DoBlockUpdate(repUpdates); 
                }
                return true;
            
            case InstEffectResourceAll instEffectResourceAll:
                ResourceType resAllType;
                var srcCard = unitSource?.UnitCard;
                if (resourceType.HasValue)
                {
                    resAllType = resourceType.Value;
                }
                else if (instEffectResourceAll.Supply)
                {
                    resAllType = ResourceType.Supply;
                }
                else if (srcCard == null)
                {
                    resAllType = ResourceType.General;
                }
                else if (srcCard.IsObjective)
                {
                    resAllType = ResourceType.Objective;
                }
                else
                {
                    resAllType = ResourceType.General;
                }
                
                if (instEffectResourceAll.IgnoreCasterPlayer && unitSource is not null)
                {
                    actualUnitList.RemoveAll(u => u.PlayerId == unitSource.OwnerPlayerId);
                }

                actualUnitList = instEffectResourceAll.AffectedTeam switch
                {
                    RelativeTeamType.Friendly => actualUnitList.Where(u => u.Team == source.Team).ToList(),
                    RelativeTeamType.Opponent => actualUnitList.Where(u => u.Team != source.Team).ToList(),
                    _ => actualUnitList
                };

                if (!instEffectResourceAll.IncludeDeadPlayers)
                {
                    actualUnitList = actualUnitList.Where(u => !u.IsDead).ToList();
                }

                if (hasImpact)
                {
                    impactData.HitUnits = actualUnitList.Select(u => u.Id).ToList();
                    ImpactOccur(impactData);
                }
                
                var amountAll = instEffectResourceAll.Resource;
                if (MathF.Abs(amountAll) < 1 && unitSource is not null)
                {
                    amountAll *= unitSource.Resource;
                }

                if (amountAll < 0)
                {
                    amountAll = MathF.Abs(amountAll);
                    actualUnitList.ForEach(u => u.RemoveResources(amountAll));    
                }
                else
                {
                    actualUnitList.ForEach(u => u.AddResource(amountAll, resAllType));
                }
                
                return true;
            
            case InstEffectSlip instEffectSlip:
                actualUnitList = actualUnitList.Where(u => u.PlayerId != null).ToList();
                var maneuver = new ManeuverSlip
                {
                    DirectionAngle = instEffectSlip.DirectionAngleRandom,
                    Distance = MathF.Abs(instEffectSlip.DistanceTo - instEffectSlip.DistanceFrom),
                    Time = instEffectSlip.Time,
                    RotationTime = instEffectSlip.RotationTime
                };
                actualUnitList.ForEach(u => _serviceZone.SendUnitManeuver(u.Id, maneuver));
                return true;
            
            case InstEffectSplashDamage instEffectSplashDamage:
                var dmgData = instEffectSplashDamage.Damage is not null
                    ? ConvertToDamageData(instEffectSplashDamage.Damage, impactPoint,
                    impactData.ShotPos, unitSource, true) : DamageData.ZeroDamage;
                
                Vector3[] locs = shift switch
                {
                    BlockShift.Left when impactPoint.X - float.Truncate(impactPoint.X) < ImpactImprecision
                        => [impactPoint, impactPoint with
                        {
                            X = impactPoint.X - ImpactImprecision
                        }],
                    BlockShift.Right when impactPoint.X - float.Truncate(impactPoint.X) > ImpactImprecisionInv
                        => [impactPoint, impactPoint with
                        {
                            X = impactPoint.X + ImpactImprecision
                        }],
                    BlockShift.Bottom when impactPoint.Y - float.Truncate(impactPoint.Y) < ImpactImprecision
                        => [impactPoint, impactPoint with
                        {
                            Y = impactPoint.Y - ImpactImprecision
                        }],
                    BlockShift.Top when impactPoint.Y - float.Truncate(impactPoint.Y) > ImpactImprecisionInv
                        => [impactPoint, impactPoint with
                        {
                            Y = impactPoint.Y + ImpactImprecision
                        }],
                    BlockShift.Back when impactPoint.Z - float.Truncate(impactPoint.Z) < ImpactImprecision
                        => [impactPoint, impactPoint with
                        {
                            Z = impactPoint.Z - ImpactImprecision
                        }],
                    BlockShift.Front when impactPoint.Z - float.Truncate(impactPoint.Z) > ImpactImprecisionInv
                        => [impactPoint, impactPoint with
                        {
                            Z = impactPoint.Z + ImpactImprecision
                        }],
                    _ => [impactPoint]
                };

                var newImpact = impactData.Clone();
                newImpact.ShotPos = impactPoint;

                var (bkUpdates, affUnits) = MapBinary.SplashDamageBlocks(locs, dmgData, newImpact,
                    instEffectSplashDamage.Radius, actualUnitList, unitSource, source.Team);

                if (bkUpdates.Count > 0)
                {
                    DoBlockUpdate(bkUpdates);
                }

                if (hasImpact)
                {
                    var hitUnits = affUnits.Select(u => u.Id).ToList();
                    impactData.HitUnits = hitUnits;
                    newImpact.HitUnits = hitUnits;
                    ImpactOccur(impactData);
                }

                if (effect.Interrupt?.Recall is true)
                {
                    foreach (var unit in affUnits.Where(u => u.IsRecall))
                    {
                        unit.EndRecall();
                        _serviceZone.SendDoCancelRecall(unit.Id);
                    }
                }

                affUnits = affUnits.Where(u => u is { IsDead: false, IsActive: true }).ToList();
                if (affUnits.Count <= 0) return true;
            
                if (instEffectSplashDamage.UnitInstEffects is { Count: > 0 })
                {
                    instEffectSplashDamage.UnitInstEffects.ForEach(ef =>
                        ApplyInstEffect(source, affUnits, ef, newImpact, shift, sourceDirection,
                            resourceType));
                }

                if (instEffectSplashDamage.UnitConstEffects is { Count: > 0 })
                {
                    affUnits.ForEach(u =>
                        u.AddEffects(
                            instEffectSplashDamage.UnitConstEffects.Select(e => new ConstEffectInfo(e)),
                            source.Team, source));
                }
                return true;
            
            case InstEffectSupply instEffectSupply:
                foreach (var unit in actualUnitList)
                {
                    unit.AddSupplies(instEffectSupply.Amount);
                }
                return true;
            
            case InstEffectTeleport instEffectTeleport:
                var validAnchors = instEffectTeleport.OwnedAnchorOnly
                    ? _units.Values.Where(u => unitSource is not null &&
                        u.OwnerPlayerId == unitSource.OwnerPlayerId &&
                        u.UnitCard?.Labels?.Contains(instEffectTeleport.Anchor) is true)
                    : _units.Values.Where(u =>
                        u.Team == source.Team &&
                        u.UnitCard?.Labels?.Contains(instEffectTeleport.Anchor) is true);
                
                if (instEffectTeleport.RangeLimit is { } limit)
                {
                    var limitSqrd = limit * limit;
                    validAnchors = validAnchors.Where(a =>
                        Vector3.DistanceSquared(a.GetMidpoint(), impactPoint) < limitSqrd);
                }

                var anchorList = validAnchors.ToList();
                anchorList.Sort((u1, u2) => u1.CreationTime.CompareTo(u2.CreationTime));
                var anchor = anchorList.First();
                var successfulTele = false;
                var aSize = anchor.UnitCard?.Size ?? Vector3s.Zero;
                foreach (var unit in actualUnitList)
                {
                    var uSize = unitSource?.UnitCard?.Size ?? Vector3s.Zero;
                    var anchorY = anchor.Transform.Position.Y - aSize.y * 0.5f +
                                                          (unit.PlayerId != null ? 0.95f : uSize.y * 0.5f);

                    var telePos = anchor.Transform.Position with { Y = anchorY };
                    if (MapBinary.GetCanFit(unit, telePos) && CollidingWithUnit(unit, telePos).All(u =>
                            (u.UnitCard?.Size ?? Vector3s.Zero) == Vector3s.Zero || u.Id == anchor.Id))
                    {
                        var teleManeuver = new ManeuverTeleport
                        {
                            Position = unit.PlayerId != null ? telePos with { Y = telePos.Y - 0.95f + UnitSpawnYOffset } : telePos
                        };
                        _serviceZone.SendUnitManeuver(unit.Id, teleManeuver);
                        successfulTele = true;
                    }
                }

                if (instEffectTeleport.DestroyAnchor)
                {
                    anchor.Killed(anchor.CreateBlankImpactData());
                }
                
                return successfulTele;
            
            case InstEffectTeleportTo when unitSource is not null:
                var midpoint = unitSource.GetMidpoint();
                var dist = Vector3.Distance(impactPoint, midpoint);
                if (dist == 0) return true;

                for (var i = 0; i < dist; i++)
                {
                    var telePos = Vector3.Lerp(impactPoint, midpoint, i / dist);
                    var doYCheck = true;
                    if (telePos.Y < _zoneData.PlanePosition)
                    {
                        telePos.Y = _zoneData.PlanePosition + UnitSizeHelper.ImprecisionVector.Y;
                        doYCheck = false;
                    }
                    
                    if (CanFit(telePos))
                    {
                        var teleToManeuver = new ManeuverTeleport
                        {
                            Position = unitSource.PlayerId != null ? telePos with { Y = telePos.Y - 0.95f } : telePos
                        };
                        _serviceZone.SendUnitManeuver(unitSource.Id, teleToManeuver);
                        return true;
                    }

                    var adjustedPosition = i == 0 ? GetShiftedPos(telePos, doYCheck) : telePos;

                    if (i == 0 && CanFit(adjustedPosition))
                    {
                        var teleToManeuver = new ManeuverTeleport
                        {
                            Position = unitSource.PlayerId != null ? adjustedPosition with { Y = adjustedPosition.Y - 0.95f } : adjustedPosition
                        };
                        _serviceZone.SendUnitManeuver(unitSource.Id, teleToManeuver);
                        return true;
                    }

                    var newPos = GetAdjustedPos(adjustedPosition);
                    if (newPos is null)
                    {
                        continue;
                    }
                    
                    if (CanFit(newPos.Value))
                    {
                        var teleToManeuver = new ManeuverTeleport
                        {
                            Position = unitSource.PlayerId != null ? newPos.Value with { Y = newPos.Value.Y - 0.95f } : newPos.Value
                        };
                        _serviceZone.SendUnitManeuver(unitSource.Id, teleToManeuver);
                        return true;
                    }
                }

                return false;

            case InstEffectUnitSpawn instEffectUnitSpawn:
                var uCard = Databases.Catalogue.GetCard<CardUnit>(instEffectUnitSpawn.UnitKey);
                if (uCard is null) return true;

                var zoneService = unitSource?.ZoneService;
                if (unitSource?.OwnerPlayerId != null &&
                    _playerIdToUnitId.TryGetValue(unitSource.OwnerPlayerId.Value, out var playerUnitId) &&
                    _playerUnits.TryGetValue(playerUnitId, out var player))
                {
                    zoneService = player.ZoneService;
                }

                var placePoint = uCard.PivotType switch
                {
                    UnitPivotType.Zero => impactPoint - (uCard.Size?.ToVector3() ?? Vector3.Zero) / 2,
                    UnitPivotType.Center => impactPoint,
                    UnitPivotType.CenterBottom or
                        UnitPivotType.PointBottom => impactPoint with
                        {
                            Y = impactPoint.Y - (uCard.Size?.y ?? 0) / 2.0f
                        },
                    _ => impactPoint
                };

                if (uCard.Size is not null && uCard.Size != Vector3s.Zero && uCard.Labels?.Contains(UnitLabel.ShieldGeneratorDestroyed) is not true)
                {
                    var adjustedPos = GetAdjustedPos(GetShiftedPos(placePoint, true));
                    if (adjustedPos is not null)
                    {
                        placePoint = adjustedPos.Value;
                    }
                }

                if (impactData.SourceKey.HasValue)
                {
                    if (CatalogueHelper.GasGrenadeKeys.Contains(impactData.SourceKey.Value))
                    {
                        placePoint = placePoint with { Y = impactPoint.Y + 0.5f };
                    }
                    else if (CatalogueHelper.NerveGasKeys.Contains(impactData.SourceKey.Value))
                    {
                        placePoint = placePoint with { Y = impactPoint.Y + 1.5f };
                    }
                } 
                
                var trnsform = new ZoneTransform
                {
                    Position = placePoint,
                    Rotation = Vector3s.Zero,
                    LocalVelocity = Vector3s.Zero
                };
                CreateUnit(uCard, trnsform, unitSource, zoneService);
                return true;
            
            case InstEffectZoneEffect instEffectZoneEffect:
                if (instEffectZoneEffect.Effects is not { Count: > 0 } eff) return true;
                var persistantSource = unitSource is not null ? new PersistOnDeathSource(unitSource, impactData) : source;
                foreach (var unit in actualUnitList)
                {
                    unit.AddEffects(eff.Select(c => new ConstEffectInfo(c, instEffectZoneEffect.Duration)),
                        source.Team, persistantSource);
                }
                return true;
            
            default:
                return true;
        }
        
        bool CanFit(Vector3 pos) => MapBinary.GetCanFit(unitSource, pos) && CollidingWithUnit(unitSource, pos).All(u =>
            (u.UnitCard?.Size ?? Vector3s.Zero) == Vector3s.Zero);
                
        bool CanFitPoint(Vector3 pos) => (!MapBinary.ContainsBlock((Vector3s)pos) ||
                                          MapBinary[(Vector3s)pos].Card.Passable == BlockPassableType.Any) &&
                                         _unitOctree.GetColliding(new BoundingBoxEx(pos, new Vector3(0.01f)))
                                             .Where(u => u.Id != unitSource.Id && u.Key != CatalogueHelper.SmokeBomb).All(u =>
                                                 (u.UnitCard?.Size ?? Vector3s.Zero) == Vector3s.Zero);

        Vector3 GetShiftedPos(Vector3 pos, bool doYCheck) =>
            shift switch
            {
                BlockShift.Left when pos.X - float.Truncate(pos.X) < ImpactImprecision
                    => pos with
                    {
                        X = pos.X - ImpactImprecision
                    },
                BlockShift.Right when pos.X - float.Truncate(pos.X) > ImpactImprecisionInv
                    => pos with
                    {
                        X = pos.X + ImpactImprecision
                    },
                BlockShift.Bottom when pos.Y - float.Truncate(pos.Y) < ImpactImprecision && doYCheck
                    => pos with
                    {
                        Y = pos.Y - ImpactImprecision
                    },
                BlockShift.Top when pos.Y - float.Truncate(pos.Y) > ImpactImprecisionInv && doYCheck
                    => pos with
                    {
                        Y = pos.Y + ImpactImprecision
                    },
                BlockShift.Back when pos.Z - float.Truncate(pos.Z) < ImpactImprecision
                    => pos with
                    {
                        Z = pos.Z - ImpactImprecision
                    },
                BlockShift.Front when pos.Z - float.Truncate(pos.Z) > ImpactImprecisionInv
                    => pos with
                    {
                        Z = pos.Z + ImpactImprecision
                    },
                _ => pos
            };
        
        Vector3? GetAdjustedPos(Vector3 pos)
        {
            var uSize = unitSource.UnitCard?.Size ?? Vector3s.Zero;
            var vecX = unitSource.PlayerId != null ? 0.25f : uSize.x * 0.5f;
            var vecY = unitSource.PlayerId != null 
                ? unitSource.Transform.IsCrouch ? 0.45f : 0.95f 
                : uSize.y * 0.5f;
            var vecZ = unitSource.PlayerId != null ? vecX : uSize.z * 0.5f;


            var fitXPos = CanFitPoint(pos with
            {
                X = pos.X + vecX - UnitSizeHelper.HalfImprecisionVector.X
            });
            
            var fitXNeg = CanFitPoint(pos with
            {
                X = pos.X - vecX + UnitSizeHelper.HalfImprecisionVector.X
            });

            if (!fitXPos && !fitXNeg)
            {
                return null;
            }
            if (!fitXPos)
            {
                pos.X = float.Floor(pos.X + vecX) - vecX;
            }
            else if (!fitXNeg)
            {
                pos.X = float.Ceiling(pos.X - vecX) + vecX;
            }
            
            var fitYPos = CanFitPoint(pos with
            {
                Y = pos.Y + vecY - UnitSizeHelper.HalfImprecisionVector.Y
            });
            
            var fitYNeg = CanFitPoint(pos with
            {
                Y = pos.Y - vecY + UnitSizeHelper.HalfImprecisionVector.Y
            });
            
            if (!fitYPos && !fitYNeg)
            {
                return null;
            }
            if (!fitYPos)
            {
                pos.Y = float.Floor(pos.Y + vecY) - vecY;
            }
            else if (!fitYNeg)
            {
                pos.Y = float.Ceiling(pos.Y - vecY) + vecY;
            }
            
            var fitZPos = CanFitPoint(pos with
            {
                Z = pos.Z + vecZ - UnitSizeHelper.HalfImprecisionVector.Z
            });
            
            var fitZNeg = CanFitPoint(pos with
            {
                Z = pos.Z - vecZ + UnitSizeHelper.HalfImprecisionVector.Z
            });
            
            if (!fitZPos && !fitZNeg)
            {
                return null;
            }
            if (!fitZPos)
            {
                pos.Z = float.Floor(pos.Z + vecZ) - vecZ;
            }
            else if (!fitZNeg)
            {
                pos.Z = float.Ceiling(pos.Z - vecZ) + vecZ;
            }

            return pos;
        }
    }

    private HashSet<ConstEffectInfo> GetTeamEffects(TeamType team) => _teamEffects[(int) team];

    private bool DoesObjBuffApply(TeamType team, IEnumerable<UnitLabel> labels) =>
        _zoneData.MatchCard.Data?.Type == MatchType.TimeTrial || !labels.Contains(
            _objectiveConquest[(int)team].Count > 0 ? _objectiveConquest[(int)team].Peek() : UnitLabel.Objective);
    
    private void ImpactOccur(ImpactData impactData)
    {
        _serviceZone.SendImpact(impactData);
    }

    private float GetResourceCap() => _gameInitiator.GetResourceCap();
    
    private void ImpactOccur(Vector3 insidePoint, Vector3 shotPos, bool crit = false, Unit? sourceUnit = null,
        Key? source = null, CardImpact? card = null, IEnumerable<uint>? affectedUnits = null, Vector3s? normal = null)
    {
        ImpactOccur(new ImpactData
        {
            InsidePoint = insidePoint,
            Normal = normal ?? Vector3s.Zero,
            CasterUnitId = sourceUnit?.Id,
            CasterPlayerId = sourceUnit?.PlayerId,
            Impact = card?.Key,
            SourceKey = source,
            HitUnits = affectedUnits?.ToList() ?? [],
            ShotPos = shotPos,
            Crit = crit
        });
    }

    private void UpdateMatchStats(Unit player, int? kills = null, int? deaths = null, int? assists = null,
        int? blocksBuilt = null, int? blocksDestroyed = null, float? resourcesEarned = null)
    {
        if (player.PlayerId == null) return;
        if (kills.HasValue)
        {
            _zoneData.PlayerStats[player.PlayerId.Value].Kills += kills.Value;
        }

        if (deaths.HasValue)
        {
            _zoneData.PlayerStats[player.PlayerId.Value].Deaths += deaths.Value;
        }

        if (assists.HasValue)
        {
            _zoneData.PlayerStats[player.PlayerId.Value].Assists += assists.Value;
        }

        if (blocksBuilt.HasValue)
        {
            _zoneData.PlayerStats[player.PlayerId.Value].BlocksBuilt += blocksBuilt.Value;
        }

        if (blocksDestroyed.HasValue)
        {
            _zoneData.PlayerStats[player.PlayerId.Value].BlocksDestroyed += blocksDestroyed.Value;
        }

        if (resourcesEarned.HasValue)
        {
            _zoneData.PlayerStats[player.PlayerId.Value].ResourcesEarned += resourcesEarned.Value;
        }
        
        ZoneUpdated(new ZoneUpdate
        {
            Statistics = new MatchStats
            {
                PlayerStats = _zoneData.PlayerStats,
                Team1Stats = _zoneData.GetTeamScores(TeamType.Team1),
                Team2Stats = _zoneData.GetTeamScores(TeamType.Team2)
            }
        });
    }

    private void UnitIsDamaged(Unit target, float damage, ImpactData impact)
    {
        var attackerPlayer = impact.CasterPlayerId is null
            ? null
            : _playerUnits.GetValueOrDefault(_playerIdToUnitId.GetValueOrDefault(impact.CasterPlayerId.Value));
        
        if (target.ProjectileUnitData is null)
        {
            _serviceZone.SendDamage(new DamageInfo
            {
                TargetUnitId = target.Id,
                SourceUnitId = attackerPlayer?.Id ?? impact.CasterUnitId,
                SourcePosition = impact.ShotPos,
                Impact = impact.Impact,
                Damage = damage,
                InitialDamage = damage,
                Crit = impact.Crit
            });
        }
        
        var attacker = impact.CasterUnitId is null ? null : _units.GetValueOrDefault(impact.CasterUnitId.Value);
        
        var targetTeam = target.Team;
        
        target.DamageStatsUpdate(targetTeam, damage, impact.Crit, impact.SourceKey == CatalogueHelper.FallImpact, attacker, attackerPlayer);

        if (target.UnitCard?.IsBase is true && target.HealthPercentage <= _zoneData.GameModeCard.Backfilling?.ObjectivesHealthThreshold)
        {
            _gameInitiator.SetBackfillReady(false);
        }
    }

    private void UnitIsKilled(Unit target, ImpactData impact, bool mining = false)
    {
        var assists = target.PlayerId != null
            ? target.RecentDamagers.Where(a => a.Value > DateTimeOffset.Now && a.Key.Team != target.Team)
                .Select(k => k.Key.PlayerId)
            .Where(a => a is not null && a != impact.CasterPlayerId).OfType<uint>().ToList() : [];
        
        var targetUnitCard = target.UnitCard;
        var isObjective = targetUnitCard?.IsObjective is true;
        
        if (target.PlayerId != null || (isObjective && impact.CasterPlayerId.HasValue))
        {
            _serviceZone.SendKill(new KillInfo
            {
                DeadUnitId = target.Id,
                Killer = impact.CasterPlayerId,
                Assistants = assists,
                DamageSource = impact.SourceKey ?? CatalogueHelper.DefaultSource,
                SourcePosition = impact.ShotPos,
                Crit = impact.Crit
            });
        }
        
        _unitsToDrop.Add(target);
        
        var killer = impact.CasterUnitId is null ? null : _units.GetValueOrDefault(impact.CasterUnitId.Value);
        var killerPlayer = impact.CasterPlayerId is null
            ? null
            : _playerUnits.GetValueOrDefault(_playerIdToUnitId.GetValueOrDefault(impact.CasterPlayerId.Value));
        
        var targetTeam = target.Team;

        if (target.IsRecall)
        {
            target.EndRecall();
            _serviceZone.SendDoCancelRecall(target.Id);
        }

        RemoveUnitFromOctree(target.Id);
        foreach (var block in target.OverlappingMapBlocks)
        {
            if (MapBinary.UnitsInsideBlock.TryGetValue(block, out var units))
            {
                units.RemoveUnit(target);
            }
        }
        
        if (target.AttachedTo is not null)
        {
            MapBinary.DetachFromBlock(target, target.AttachedTo.Value);
            target.AttachedTo = null;
        }

        if (target.SpawnId is not null)
        {
            _playerSpawnPoints.Remove(target.SpawnId.Value);
            _zoneData.RemoveSpawn(target.SpawnId.Value);
        }

        if (target is { IsActive: true })
        {
            var assisters = assists
                .Where(i => _playerIdToUnitId.ContainsKey(i) && _playerUnits.ContainsKey(_playerIdToUnitId[i]))
                .Select(i => _playerUnits[_playerIdToUnitId[i]]);
            
            target.KillStatsUpdate(targetTeam, impact.Crit, killer, killerPlayer, assisters);
            if (target is { PlayerId: not null })
            {
                UpdateRespawnTime(target);
            }
        }
        
        killer ??= killerPlayer;
        var killerTeam = killer?.Team;
        
        switch (targetUnitCard?.Data)
        {
            case UnitDataBomb { TriggerEffect: not null } unitDataBomb when target.IsFuseExpired:
                var tImpact1 = target.CreateImpactData();
                ApplyInstEffect(target.GetSelfSource(tImpact1), [], unitDataBomb.TriggerEffect, tImpact1, BlockShift.Top);
                break;
            
            case UnitDataDrill { DeathEffect: not null } unitDataDrill:
                var tImpact2 = target.CreateImpactData();
                ApplyInstEffect(target.GetSelfSource(tImpact2), [], unitDataDrill.DeathEffect, tImpact2);
                break;
            
            case UnitDataLandmine { TriggerEffect: not null, HitOnTimeout: true } unitDataLandmine:
                var tImpact3 = target.CreateImpactData();
                ApplyInstEffect(target.GetSelfSource(tImpact3),
                    _unitOctree.GetColliding(new BoundingSphere(target.GetMidpoint(),
                        unitDataLandmine.TriggerRadius)), unitDataLandmine.TriggerEffect,
                    tImpact3);
                break;
            
            case UnitDataLandmine { TriggerEffect: not null } unitDataLandmine when !target.IsExpired:
                var tImpact4 = target.CreateImpactData();
                ApplyInstEffect(target.GetSelfSource(tImpact4), _unitOctree.GetColliding(new BoundingSphere(target.GetMidpoint(),
                    unitDataLandmine.TriggerRadius)), unitDataLandmine.TriggerEffect, tImpact4);
                break;
            
            case UnitDataPickup { TakeEffect: not null } unitDataPickup when killer is not null:
                target.Team = killer.Team;
                var tImpact5 = target.CreateImpactData();
                killerPlayer?.StatsFromPickup(target);
                ApplyInstEffect(target.GetSelfSource(tImpact5), [killer], unitDataPickup.TakeEffect, tImpact5);
                break;
            
            case UnitDataPiggyBank unitDataPiggyBank when killerPlayer is not null:
                var resourceCount = (float)(target.TimeSinceCreated.TotalSeconds *
                                            unitDataPiggyBank.ResourcePerInterval /
                                            (unitDataPiggyBank.GenerationInterval * 5));
            
                foreach(var player in _playerUnits.Values.Where(p => p.Team == killerPlayer.Team && !p.IsDead))
                {
                    player.AddResource(resourceCount, ResourceType.General);
                }
                break;
            
            case UnitDataPlayer when killerPlayer is not null && killerPlayer.Team != target.Team:
                var selfImpact = killerPlayer.CreateImpactData();
                foreach (var effect in killerPlayer.ActiveEffects.GetEffectsOfType<ConstEffectOnKill>().Select(kil => kil.Effect).OfType<InstEffect>())
                {
                    ApplyInstEffect(killerPlayer.GetSelfSource(selfImpact), [killerPlayer], effect, selfImpact);
                }
                break;
            
            case UnitDataPortal when target.PortalLinked.LinkedPortalUnitId is not null:
                var otherPortal = _units.GetValueOrDefault(target.PortalLinked.LinkedPortalUnitId.Value);
                otherPortal?.UpdateData(new UnitUpdate
                {
                    PortalLink = new PortalLink
                    {
                        LinkedPortalUnitId = null
                    }
                });
                break;
            
            case UnitDataProjectile { DeathEffect: not null } unitDataProjectile:
                var tImpact6 = target.CreateImpactData();
                ApplyInstEffect(target.GetSelfSource(tImpact6), [], unitDataProjectile.DeathEffect, tImpact6);
                break;
        }
        
        if (targetUnitCard?.Health?.KillReward is {} reward && killerPlayer is not null && (!reward.Mining || mining))
        {
            if (reward.TeamReward is not null && (target.PlayerId == null || killerPlayer.Team != targetTeam))
            {
                foreach(var player in _playerUnits.Values.Where(p => p.Team == killerPlayer.Team && p != killerPlayer))
                {
                    player.AddResource(reward.TeamReward.Value,
                        target.PlayerId != null ? ResourceType.TeamKill :
                        isObjective ? ResourceType.Objective : ResourceType.General);
                }
            }
            if (reward.EnemyReward is not null && killerPlayer.Team != targetTeam)
            {
                var destroyValue = Unit.ResolveDeviceResourceValue(targetUnitCard, reward.EnemyReward,
                    reward.PlayerReward);
                killerPlayer.AddResource(reward.EnemyReward.Value,
                    target.PlayerId != null ? ResourceType.Kill :
                    isObjective ? ResourceType.Objective :
                    mining ? ResourceType.Mining : ResourceType.General);

                if (targetUnitCard.Health.Health?.HealthType is HealthType.World)
                {
                    killerPlayer.DestroyedBlock(targetUnitCard.DeviceType, destroyValue);
                }
            }
            else if (reward.PlayerReward is not null)
            {
                var destroyValue = Unit.ResolveDeviceResourceValue(targetUnitCard, reward.EnemyReward,
                    reward.PlayerReward);
                killerPlayer.AddResource(reward.PlayerReward.Value,
                    target.PlayerId != null ? ResourceType.Kill :
                    isObjective ? ResourceType.Objective :
                    mining ? ResourceType.Mining : ResourceType.General);

                if (targetUnitCard.Health.Health?.HealthType is HealthType.World)
                {
                    killerPlayer.DestroyedBlock(targetUnitCard.DeviceType, destroyValue);
                }
            }
        }

        if (isObjective)
        {
            if (_objectiveConquest[(int)target.Team].Count > 0 &&
                targetUnitCard?.Labels?.Contains(_objectiveConquest[(int)target.Team].Peek()) is true)
            {
                _objectiveConquest[(int)target.Team].Dequeue();
                if (_objectiveConquest[(int)target.Team].Count > 0)
                {
                    var nextLabel = _objectiveConquest[(int)target.Team].Peek();
                    foreach (var obj in _units.Values.Where(u => u.Team == target.Team && u.UnitCard?.Labels?.Contains(nextLabel) is true))
                    {
                        obj.ActiveEffects = obj.ActiveEffects.RemoveAll(e => CatalogueHelper.ObjectiveShieldKeys.Contains(e.Key));
                    }
                }
            
                foreach (var mapSpawnPoint in
                         _mapSpawnPoints.Where(s => s.Value.Label is SpawnPointLabel.Objective1))
                {
                    _zoneData.UpdateSpawn(mapSpawnPoint.Key, IsMapSpawnRequirementsMet(mapSpawnPoint.Value));
                }
            }

            if (_endMatchTask is null && CheckIfMatchOver(targetTeam, killerPlayer))
            { 
                _endMatchTask = _zoneData.MatchCard.Data?.Type switch
                {
                    MatchType.ShieldRush2 or
                        MatchType.ShieldCapture => BeginEndOfGame(targetTeam switch
                                                   {
                                                       TeamType.Neutral => TeamType.Team1,
                                                       TeamType.Team1 => TeamType.Team2,
                                                       TeamType.Team2 => TeamType.Team1,
                                                       _ => TeamType.Neutral
                                                   }),
                    MatchType.Tutorial or
                        MatchType.TimeTrial => BeginEndOfGame(killerPlayer?.Team ?? TeamType.Neutral, false),
                    _ => null
                };
            }
        }

        if (_zoneData.MatchCard.Data?.Type is MatchType.TimeTrial or MatchType.Tutorial)
        {
            _zoneData.CheckIfObjective(target, killerTeam ?? TeamType.Neutral);
            if (_endMatchTask is null && CheckIfMatchOver(targetTeam, killerPlayer))
            {
                _endMatchTask = BeginEndOfGame(killerPlayer?.Team ?? TeamType.Neutral, false);
            }
        }
        
        if (targetUnitCard?.Loot is not { LootItem: not null } loot) return;
        if (killer is null)
        {
            if (!loot.SpawnOnUndefinedKill) 
                return;
        }
        else if ((killerTeam == targetTeam && !loot.SpawnOnFriendlyKill) || (killerTeam != targetTeam && !loot.SpawnOnEnemyKill)) 
            return;

        var lootPos = ZoneTransformHelper.ToZoneTransform(target.Transform.Position, Quaternion.Identity);

        switch (loot.LootItem)
        {
            case LootItemCommon lootItemCommon:
                var lootItem = killerTeam == targetTeam
                    ? lootItemCommon.Item
                    : lootItemCommon.OpponentItem ?? lootItemCommon.Item;

                if (lootItem is null) return;
                
                CreateLootUnit(lootItem, lootPos, killer);
                break;
            
            case LootItemCondition lootItemCondition:
                var lootItems = killerTeam == targetTeam
                    ? lootItemCondition.ItemsByCondition
                    : lootItemCondition.OpponentItemsByCondition ?? lootItemCondition.ItemsByCondition;

                if (lootItems is null) return;
                
                if (killer is null)
                {
                    if (lootItems.TryGetValue(LootConditionType.Always, out var alwaysItem))
                    {
                        CreateLootUnit(alwaysItem, lootPos, killer);
                    }
                }
                else
                {
                    if (lootItems.TryGetValue(LootConditionType.LowHealth, out var lowHpLoot) && killer.IsLowHealth)
                    {
                        CreateLootUnit(lowHpLoot, lootPos, killer);
                    }
                    else if (lootItems.TryGetValue(LootConditionType.LowAmmo, out var lowAmmoLoot) && killer.IsLowAmmo)
                    {
                        CreateLootUnit(lowAmmoLoot, lootPos, killer);
                    }
                    else if (lootItems.TryGetValue(LootConditionType.FriendlySide, out var friendSideLoot) &&
                             MapBinary.OnFriendlySide(target.Transform.Position, killer.Team))
                    {
                        CreateLootUnit(friendSideLoot, lootPos, killer);
                    }
                    else if (lootItems.TryGetValue(LootConditionType.EnemySide, out var enemySideLoot) &&
                             MapBinary.OnEnemySide(target.Transform.Position, killer.Team))
                    {
                        CreateLootUnit(enemySideLoot, lootPos, killer);
                    }
                    else if (lootItems.TryGetValue(LootConditionType.Always, out var alwaysLoot))
                    {
                        CreateLootUnit(alwaysLoot, lootPos, killer);
                    }
                }
                break;
            
            case LootItemRandom lootItemRandom:
                var lootItemsRand = killerTeam == targetTeam
                    ? lootItemRandom.ItemsByWeight
                    : lootItemRandom.OpponentItemsByWeight ?? lootItemRandom.ItemsByWeight;
                
                if (lootItemsRand is null) return;
                
                var randSelector = new Random();
                var randVal = randSelector.NextSingle();
                var accumulatedWeight = 0.0f;

                foreach (var weightedLootItem in lootItemsRand)
                {
                    accumulatedWeight += weightedLootItem.Weight;
                    if (randVal <= accumulatedWeight && weightedLootItem.Item is not null)
                    {
                        CreateLootUnit(weightedLootItem.Item, lootPos, killer);
                        break;
                    }
                }
                break;
            
            default:
                return;
        }
    }

    private void DropUnit(Unit unit)
    {
        if (unit.PlayerId is null)
        {
            RemoveUnit(unit.Id); 
        }
        
        _unbufferedZone.SendUnitDrop(unit.Id);
        unit.IsDropped = true;
    }

    private uint ChangeId(Unit unit)
    {
        var newId = NewUnitId();
        var oldId = unit.Id;
        _units.Remove(oldId);
        if (unit.PlayerId is not null)
        {
            _playerUnits.Remove(oldId);
            _playerIdToUnitId[unit.PlayerId.Value] = newId;
            _playerUnits.Add(newId, unit);
        }
        _units.Add(newId, unit);
        
        return newId;
    }

    private void LinkPortal(Unit unit, bool unlink = false)
    {
        if (unlink)
        {
            if (unit.PortalLinked.LinkedPortalUnitId is null) return;
            var linkedPortal = _units.GetValueOrDefault(unit.PortalLinked.LinkedPortalUnitId.Value);
            if (linkedPortal is null) return;
            var blankLink = new PortalLink
            {
                LinkedPortalUnitId = null
            };
            
            unit.UpdateData(new UnitUpdate
            {
                PortalLink = blankLink
            });
            linkedPortal.UpdateData(new UnitUpdate
            {
                PortalLink = blankLink
            });
        }
        else
        {
            var portalCandidate = _units.Values.FirstOrDefault(u =>
                u.UnitCard?.Data is UnitDataPortal && u.Key == unit.Key && u.Id != unit.Id &&
                u.PortalLinked.LinkedPortalUnitId is null && u.OwnerPlayerId == unit.OwnerPlayerId &&
                !u.IsBuff(BuffType.Disabled));
        
            if (portalCandidate is null) return;
        
            var thisPortalLink = unit.PortalLinked;
            thisPortalLink.LinkedPortalUnitId = portalCandidate.Id;
            var thatPortalLink = portalCandidate.PortalLinked;
            thatPortalLink.LinkedPortalUnitId = unit.Id;
            unit.UpdateData(new UnitUpdate
            {
                PortalLink = thisPortalLink
            });
            portalCandidate.UpdateData(new UnitUpdate
            {
                PortalLink = thatPortalLink
            });
        }
    }

    private void OnDisarmed(Unit unit)
    {
        if (unit is not { CurrentBuildInfo: not null, PlayerId: not null }) return;
        
        ReceivedCancelBuildRequest(unit.PlayerId.Value);
    }

    private void OnPull(Unit unit, ManeuverPull maneuverPull)
    {
        if (!unit.IsBuff(BuffType.Root))
            _serviceZone.SendUnitManeuver(unit.Id, maneuverPull);
    }

    private void OnShower(Unit unit, UnitDataShower shower, Random rand)
    {
        var r = shower.Radius * MathF.Sqrt(rand.NextSingle());
        var theta = rand.NextSingle() * 2 * MathF.PI;
        
        var xVal = (int)float.FusedMultiplyAdd(r, MathF.Cos(theta), unit.Transform.Position.X);
        var zVal = (int)float.FusedMultiplyAdd(r, MathF.Sin(theta), unit.Transform.Position.Z);
        
        if (xVal == (int)unit.Transform.Position.X && zVal == (int)unit.Transform.Position.Z) return;
        
        var blockPos = MapBinary.GetGroundBlockFromSky(xVal, zVal);
        if (blockPos is null || shower.HitEffect is null) return;

        var blockBottom = CoordsHelper.BlockBottom(blockPos.Value);
        var placeImpact = unit.CreateImpactData(insidePoint: blockBottom with { Y = blockBottom.Y + 0.99f });
        ApplyInstEffect(unit.GetSelfSource(placeImpact), [], shower.HitEffect, placeImpact, BlockShift.Top);
    }
    
    private void UpdateSpawnPoint(Unit unit)
    {
        if (unit.SpawnId is null) return;
        
        _zoneData.UpdateSpawn(unit.SpawnId.Value, CheckSpawn(unit, unit.Transform.Position), unit.OwnerPlayerId, true,
            unit.Transform.Position, unit.Team);
    }

    private static void OnMatchContextChanged(Unit unit, TeamType oldTeam, TeamType newTeam, ConstEffectOnMatchContext effect)
    {
        if (oldTeam == newTeam || unit.Team == TeamType.Neutral) return;

        var source = unit.GetSelfSource(unit.CreateImpactData());
        if (oldTeam == unit.Team && effect.EffectsOnLeading is { Count: > 0 })
        {
            unit.RemoveEffects(effect.EffectsOnLeading.Select(e => new ConstEffectInfo(e)), unit.Team, source);
        }
        else if (oldTeam != unit.Team && oldTeam != TeamType.Neutral && effect.EffectsOnLosing is { Count: > 0 })
        {
            unit.RemoveEffects(effect.EffectsOnLosing.Select(e => new ConstEffectInfo(e)), unit.Team, source);
        }

        if (newTeam == unit.Team && effect.EffectsOnLeading is { Count: > 0 })
        {
            unit.AddEffects(effect.EffectsOnLeading.Select(e => new ConstEffectInfo(e)), unit.Team, source);
        }
        else if (newTeam != unit.Team && newTeam != TeamType.Neutral && effect.EffectsOnLosing is { Count: > 0 })
        {
            unit.AddEffects(effect.EffectsOnLosing.Select(e => new ConstEffectInfo(e)), unit.Team, source);
        }
    }

    private Unit? GetPlayerFromPlayerId(uint playerId)
    {
        if (!_playerIdToUnitId.TryGetValue(playerId, out var playerUnitId) ||
            !_playerUnits.TryGetValue(playerUnitId, out var player))
        {
            return null;
        }
        
        return player;
    }
    
    private OnUnitInit GetUnitInitAction(IServiceZone? creatorService = null) =>
        (unit, init) => UnitCreated(unit, init, creatorService);

    private void OnCut(uint attackerId, float totalRes)
    {
        if (!_playerIdToUnitId.TryGetValue(attackerId, out var attackerUnitId) || !_playerUnits.TryGetValue(attackerUnitId, out var attacker)) return;
        if (attacker.PlayerId == null) return;
        var matchCard = _zoneData.MatchCard;
        var affectedPlayers = matchCard.FallingBlocksLogic?.ApplyRewardToWholeTeam ?? true
            ? _playerUnits.Values.Where(p => p.Team == attacker.Team).ToList()
            : [attacker];
        
        totalRes *= MathF.Pow(matchCard.FallingBlocksLogic?.ResourceCoeff ?? 1.0f, 2);

        if (matchCard.FallingBlocksLogic?.ResourceCap is { } cap)
        {
            totalRes = MathF.Min(totalRes, cap);
        }
        
        affectedPlayers.ForEach(p => p.AddResource(totalRes, ResourceType.Mining));
    }

    private void OnMined(uint attackerId, Key blockKey)
    {
        _serviceZone.SendBlockMined(attackerId, blockKey);
    }

    private void OnDetached(Unit unit)
    {
        unit.AttachedTo = null;
        if (unit.UnitCard?.BlockBinding == UnitBlockBindingType.Destroy)
        {
            var impact = unit.CreateBlankImpactData();
            EnqueueAction(() => unit.Killed(impact));
        }
        else
        {
            MovementActive(unit);
        }
    }
    
    private (List<uint>? teslas, uint? target, uint tesla) PropagateTesla(Unit tesla, List<Unit> propPath, HashSet<Unit> checkedTeslas)
    {
        if (tesla.TeslaUnitData is not {} data) return (null, null, tesla.Id);
        var unitsNearby = _unitOctree.GetColliding(new BoundingSphere(tesla.GetMidpoint(), data.AttackRange));
        var nearbyEnemies = MapBinary.CheckVisibility(tesla.GetMidpoint(), 
                unitsNearby.Where(u => data.AttackTargeting is null || u.DoesEffectApply(data.AttackTargeting, tesla.Team)), 
                unitsNearby.Where(u => u != tesla && !checkedTeslas.Contains(u) && u.PlayerId is null).ToList())
            .Select(u => u?.Id);

        var target = nearbyEnemies.FirstOrDefault();
        if (target is not null)
        {
            return (propPath.Select(u => u.Id).ToList(), target, tesla.Id);
        }

        var propNearby = _unitOctree.GetColliding(new BoundingSphere(tesla.GetMidpoint(), data.PropagationRange))
            .Where(u => u != tesla && !checkedTeslas.Contains(u) && u.PlayerId is null).ToList();

        var nearbyTeslas = MapBinary.CheckVisibility(tesla.GetMidpoint(), 
                propNearby.Where(u => u.TeslaUnitData is not null && u.OwnerPlayerId == tesla.OwnerPlayerId), 
                propNearby);
        
        var newProp = propPath.ToList();
        newProp.Add(tesla);
        checkedTeslas.Add(tesla);
        
        var solutions = nearbyTeslas.Select(t => PropagateTesla(t, newProp, checkedTeslas)).ToList();
        return solutions.Any(t => t.teslas is not null) ? solutions.MinBy(t => t.teslas?.Count) : (null, null, tesla.Id);
    }
    
    private void ZoneUpdated(ZoneUpdate update)
    {
        if (_sessionsSender.SenderCount == 0) return;
        if (_gameLoop != null)
        {
            _serviceZone.SendUpdateZone(update);
        }
        else
        {
            _unbufferedZone.SendUpdateZone(update);
        }
    }
}