using System.Numerics;
using BNLReloadedServer.Service;

namespace BNLReloadedServer.BaseTypes;

public delegate void OnUnitInit(Unit unit, UnitInit unitInit);
public delegate void OnUnitUpdate(Unit unit, UnitUpdate unitUpdate, bool unbuffered = false);
public delegate void OnUnitMove(Unit unit, ulong time, ZoneTransform transform, Vector3 oldPosition);
public delegate void OnTeamEffectAdded(Unit unit, ConstEffectInfo effectInfo);
public delegate void OnTeamEffectRemoved(Unit unit, ConstEffectInfo effectInfo);
public delegate bool OnApplyInstEffect(EffectSource source, IEnumerable<Unit> affectedUnits, InstEffect effect,
    ImpactData impactData, BlockShift? shift = null, Direction2D? sourceDirection = null, ResourceType? resourceType = null,
    bool damageBlock = true);
public delegate IEnumerable<ConstEffectInfo> GetTeamEffects(TeamType team);
public delegate bool DoesObjBuffApply(TeamType team, IEnumerable<UnitLabel> labels);
public delegate void OnImpactAction(Vector3 insidePoint, Vector3 shotPos, bool crit = false, Unit? sourceUnit = null,
    Key? source = null, CardImpact? card = null, IEnumerable<uint>? affectedUnits = null, Vector3s? normal = null);
public delegate float GetResourceCap();
public delegate void UpdateMatchStats(Unit player, int? kills = null, int? deaths = null, int? assists = null,
    int? blocksBuilt = null, int? blocksDestroyed = null, float? resourcesEarned = null);
public delegate void OnUnitDamaged(Unit target, float damage, ImpactData impact);
public delegate void OnUnitKilled(Unit target, ImpactData impact, bool mining = false);
public delegate void LinkPortal(Unit unit, bool unlink = false);
public delegate void OnPull(Unit unit, ManeuverPull maneuverPull);
public delegate void OnRespawn(Unit unit, UnitInit unitInit, IServiceZone creatorService);
public delegate void OnDisarmed(Unit unit);
public delegate uint OnChangeId(Unit unit);
public delegate Unit? GetPlayerFromPlayerId(uint playerId);
public delegate bool EnqueueAction(Action action);

public record UnitUpdater(
    OnUnitInit OnUnitInit,
    OnUnitUpdate OnUnitUpdate,
    OnUnitMove OnUnitMove,
    OnTeamEffectAdded OnTeamEffectAdded,
    OnTeamEffectRemoved OnTeamEffectRemoved,
    OnApplyInstEffect OnApplyInstEffect,
    GetTeamEffects GetTeamEffects,
    DoesObjBuffApply DoesObjBuffApply,
    OnImpactAction OnImpactOccur,
    GetResourceCap GetResourceCap,
    UpdateMatchStats UpdateMatchStats,
    OnUnitDamaged OnUnitDamaged,
    OnUnitKilled OnUnitKilled,
    LinkPortal LinkPortal,
    OnPull OnPull,
    OnRespawn OnRespawn,
    OnDisarmed OnDisarmed,
    OnChangeId OnChangeId,
    GetPlayerFromPlayerId GetPlayerFromPlayerId,
    EnqueueAction EnqueueAction);