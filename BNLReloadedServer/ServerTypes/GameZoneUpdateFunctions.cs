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
        _units.Add(unit.Id, unit);
        if (unitInit.PlayerId != null)
        {
            _playerUnits.Add(unit.Id, unit);
            _playerIdToUnitId.Add(unitInit.PlayerId.Value, unit.Id);
        }
        
        if (unitInit.Transform is not null)
        {
            AddUnitToOctree(unit, unitInit.Transform);
        }
        
        if (_sessionsSender.SenderCount == 0 && unit.PlayerId == null) return;
        if (unitInit.Controlled)
        {
            _serviceZone.SendUnitCreate(unit.Id, unitInit);
        }
        else
        {
            _unbufferedZone.SendUnitCreate(unit.Id, unitInit);
            if (_gameLoop != null)
            {
                creatorService?.SendUnitControl(unit.Id);
            }
            else
            {
                unitInit.Controlled = true;
                creatorService?.SendUnitCreate(unit.Id, unitInit);
            }
        }
    }

    private void UnitUpdated(Unit unit, UnitUpdate unitUpdate)
    {
        if (_sessionsSender.SenderCount == 0) return;
        if (_gameLoop != null)
        {
            _serviceZone.SendUnitUpdate(unit.Id, unitUpdate); 
        }
        else
        {
            _unbufferedZone.SendUnitUpdate(unit.Id, unitUpdate);
        }
    }

    private void UnitMoved(Unit unit, ulong time, ZoneTransform transform)
    {
        _unitOctree.Remove(unit);
        AddUnitToOctree(unit, transform);
        _serviceZone.SendUnitMove(unit.Id, time, transform);
    }

    private void UnitTeamEffectAdded(Unit unit, ConstEffectInfo effectInfo)
    {
        IEnumerable<Unit> teamUnits;
        if (effectInfo.Card.Effect is not { } constEffect) return;
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
            element.AddEffect(effectInfo, unit.Team);
        }
    }

    private void UnitTeamEffectRemoved(Unit unit, ConstEffectInfo effectInfo)
    {
        IEnumerable<Unit> teamUnits;
        if (effectInfo.Card.Effect is not { } constEffect) return;
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
            element.RemoveEffect(effectInfo, unit.Team);
        }
    }
    
    private bool ApplyInstEffect(Unit source, IEnumerable<Unit> affectedUnits, InstEffect effect, bool enqueueAction,
        ImpactData impactData, BlockShift? shift = null, Direction2D? sourceDirection = null, ResourceType? resourceType = null)
    {
        if (enqueueAction)
        {
            EnqueueAction(() => ApplyInstEffect(source, affectedUnits, effect, false, impactData, shift, sourceDirection, resourceType));
            return true;
        }

        var actualUnits = affectedUnits;
        if (effect.Targeting?.IgnoreCaster ?? false)
        {
            actualUnits = actualUnits.Except([source]);
        }

        if (effect.Targeting?.AffectedLabels is { Count: > 0 } labels)
        {
            actualUnits = actualUnits.Where(u => u.UnitCard?.Labels?.Intersect(labels).Any() ?? false);
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

        if (effect.Impact is { } imp && Databases.Catalogue.GetCard<CardImpact>(imp) is { } impact)
        {
            impactData.HitUnits = actualUnitList.Select(u => u.Id).ToList();
            impactData.Impact = impact.Key;
            ImpactOccur(impactData);
        }

        switch (effect)
        {
            case InstEffectAddAmmo instEffectAddAmmo:
                actualUnitList.ForEach(u => u.AddAmmo(instEffectAddAmmo.Amount));
                return true;
            
            case InstEffectAddAmmoPercent instEffectAddAmmoPercent:
                actualUnitList.ForEach(u => u.AddAmmoPercent(instEffectAddAmmoPercent.Fraction));
                return true;
            
            case InstEffectAddResource instEffectAddResource:
                ResourceType resType;
                var sourceCard = source.UnitCard;
                if (resourceType.HasValue)
                {
                    resType = resourceType.Value;
                }
                else if (sourceCard == null)
                {
                    resType = ResourceType.General;
                }
                else if (sourceCard.IsObjective)
                {
                    resType = ResourceType.Objective;
                }
                else if (instEffectAddResource.Supply)
                {
                    resType = ResourceType.Supply;
                }
                else
                {
                    resType = ResourceType.General;
                }
                
                actualUnitList.ForEach(u => u.AddResource(instEffectAddResource.Amount, resType));
                return true;
            
            case InstEffectAllPlayersPersistent instEffectAllPlayersPersistent:
                var players = instEffectAllPlayersPersistent switch
                {
                    { AffectedTeam: RelativeTeamType.Friendly } => _playerUnits.Where(p => p.Value.Team == source.Team),
                    { AffectedTeam: RelativeTeamType.Opponent } => _playerUnits.Where(p => p.Value.Team != source.Team),
                    _ => _playerUnits
                };

                if (!instEffectAllPlayersPersistent.IncludeDeadPlayers)
                {
                    players = players.Where(p => !p.Value.IsDead);
                }

                var constEffects = instEffectAllPlayersPersistent.Constant?.Select(c =>
                    new ConstEffectInfo(c, instEffectAllPlayersPersistent.PersistenceDuration));
                if (constEffects != null)
                {
                    players.ToList().ForEach(player => player.Value.AddEffects(constEffects.ToList(), source.Team));
                }
                return true;
            
            case InstEffectAllUnitsBunch { Range: not null } instEffectAllUnitsBunch:
                var units = _unitOctree.GetColliding(new BoundingSphere(impactData.InsidePoint, instEffectAllUnitsBunch.Range.Value));
                if (instEffectAllUnitsBunch.Constant is { } constant)
                {
                   foreach (var unit in units)
                   {
                       unit.AddEffects(constant.Select(c => new ConstEffectInfo(c)).ToList(), source.Team);
                   } 
                }

                if (instEffectAllUnitsBunch.Instant is not { } instant) return true;
                return !instant.Any(ins => !ApplyInstEffect(source, units, ins, false, impactData, shift, sourceDirection, resourceType) &&
                                            instEffectAllUnitsBunch.BreakOnEffectFail);

            case InstEffectBlocksSpawn { Pattern: not null } instEffectBlocksSpawn:
                var addUpdates =
                    _zoneData.BlocksData.AddBlocks(instEffectBlocksSpawn.Pattern, impactData.InsidePoint, shift, source);
                if (addUpdates.Count > 0)
                {
                   _unbufferedZone.SendBlockUpdates(addUpdates); 
                }
                return true;
            
            case InstEffectBuildDevice instEffectBuildDevice:
                if (source.Resource < instEffectBuildDevice.TotalCost) return false;
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
                            source.CurrentBuildInfo?.Direction ?? Direction2D.Left, source);
                        if (updates.Count <= 0) return true;
                        
                        _unbufferedZone.SendBlockUpdates(updates);
                        source.RemoveResources(instEffectBuildDevice.TotalCost);
                        if (source.PlayerId is not null)
                        {
                            _serviceZone.SendDeviceBuilt(source.PlayerId.Value, devCard.Key, blockLoc.ToVector3() + new Vector3(0.5f));
                        }
                        return true;
                    case CardUnit unitCard:
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
                            if (_zoneData.BlocksData[checkPosition].Card.IsVisualSlope &&
                                UnitSizeHelper.IsInsideUnit(checkPosition, source) || _unitOctree.GetColliding(
                                    new BoundingBoxEx(
                                    checkPosition.ToVector3() + unitCard.Size.Value.ToVector3() * 0.5f,
                                    unitCard.Size.Value.ToVector3())).Except([source]).Any()) 
                                return false;
                            
                            collision = UnitSizeHelper.IsInsideUnit(blockLoc, source) || _unitOctree.GetColliding(
                                new BoundingBoxEx(
                                blockLoc.ToVector3() + unitCard.Size.Value.ToVector3() * 0.5f,
                                unitCard.Size.Value.ToVector3())).Except([source]).Any();
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
                        CreateUnit(unitCard, transform, source, source.ZoneService);
                        var blkUpdates = _zoneData.BlocksData.RemoveBlock(blockLoc);
                        var blkUpdates2 =
                            _zoneData.BlocksData.MakeSlopeSolid(blockLoc, checkPosition);
                        foreach (var upd in blkUpdates2)
                        {
                            blkUpdates[upd.Key] = upd.Value;
                        }
                        source.RemoveResources(instEffectBuildDevice.TotalCost);
                        if (blkUpdates.Count > 0)
                        {
                            _unbufferedZone.SendBlockUpdates(blkUpdates);
                        }
                        if (source.PlayerId is not null)
                            _serviceZone.SendDeviceBuilt(source.PlayerId.Value, devCard.Key, placePos);
                        return true;
                    default:
                        return false;
                }
                
            case InstEffectBunch instEffectBunch:
                if (instEffectBunch.Constant is { } con)
                {
                    foreach (var unit in actualUnitList)
                    {
                        unit.AddEffects(con.Select(c => new ConstEffectInfo(c)).ToList(), source.Team);
                    } 
                }
                if (instEffectBunch.Instant is not { } inst) return true;
                return !inst.Any(ins => !ApplyInstEffect(source, actualUnitList, ins, false, impactData, shift, sourceDirection, resourceType) &&
                                            instEffectBunch.BreakOnEffectFail);
            
            case InstEffectCasterBunch instEffectCasterBunch:
                if (instEffectCasterBunch.Constant is { } cons)
                {
                    source.AddEffects(cons.Select(c => new ConstEffectInfo(c)).ToList(), source.Team);
                }
                if (instEffectCasterBunch.Instant is not { } insta) return true;
                return !insta.Any(ins => !ApplyInstEffect(source, [source], ins, false, impactData, shift, sourceDirection, resourceType) &&
                                         instEffectCasterBunch.BreakOnEffectFail);
            
            case InstEffectChargeTesla instEffectChargeTesla:
                return true;
            case InstEffectDamage instEffectDamage:
                return true;
            case InstEffectDamageBlocks instEffectDamageBlocks:
                return true;
            case InstEffectDrainAmmo instEffectDrainAmmo:
                return true;
            case InstEffectDrainMagazineAmmo instEffectDrainMagazineAmmo:
                return true;
            case InstEffectFireMortars instEffectFireMortars:
                return true;
            case InstEffectHeal instEffectHeal:
                return true;
            case InstEffectHealBlocks instEffectHealBlocks:
                return true;
            case InstEffectInstReload instEffectInstReload:
                return true;
            case InstEffectKill instEffectKill:
                return true;
            case InstEffectKnockback instEffectKnockback:
                return true;
            case InstEffectPurge instEffectPurge:
                return true;
            
            case InstEffectReplaceBlocks instEffectReplaceBlocks:
                var repUpdates = _zoneData.BlocksData.ReplaceBlocks(instEffectReplaceBlocks.ReplaceWith,
                    instEffectReplaceBlocks.Range, impactData.InsidePoint, source);
                if (repUpdates.Count > 0)
                {
                    _unbufferedZone.SendBlockUpdates(repUpdates); 
                }
                return true;
            
            case InstEffectResourceAll instEffectResourceAll:
                return true;
            case InstEffectSlip instEffectSlip:
                return true;
            case InstEffectSplashDamage instEffectSplashDamage:
                return true;
            case InstEffectSupply instEffectSupply:
                return true;
            case InstEffectTeleport instEffectTeleport:
                return true;
            case InstEffectTeleportTo instEffectTeleportTo:
                return true;
            case InstEffectUnitSpawn instEffectUnitSpawn:
                return true;
            case InstEffectZoneEffect instEffectZoneEffect:
                return true;
            default:
                return true;
        }
    }

    private IEnumerable<ConstEffectInfo> GetTeamEffects(TeamType team) => _teamEffects[(int) team];

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

    private void UpdateMatchStats(Unit player, int? kills = null, int? deaths = null, int? assists = null)
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

    private OnUnitInit GetUnitInitAction(IServiceZone? creatorService = null) =>
        (unit, init) => UnitCreated(unit, init, creatorService);

    private void OnCut(uint attackerId, float totalRes)
    {
        if (!_playerUnits.TryGetValue(_playerIdToUnitId[attackerId], out var attacker)) return;
        if (attacker.PlayerId == null) return;
        var matchCard = _zoneData.MatchCard;
        var affectedPlayers = matchCard.FallingBlocksLogic?.ApplyRewardToWholeTeam ?? true
            ? _playerUnits.Values.Where(p => p.Team == attacker.Team).ToList()
            : [attacker];
        
        totalRes *= matchCard.FallingBlocksLogic?.ResourceCoeff ?? 1.0f;

        if (matchCard.FallingBlocksLogic?.ResourceCap is { } cap)
        {
            totalRes = MathF.Min(totalRes, cap);
        }
        
        affectedPlayers.ForEach(p => p.AddResource(totalRes, ResourceType.Mining));
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