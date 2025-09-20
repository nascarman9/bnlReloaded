using System.Numerics;

namespace BNLReloadedServer.BaseTypes;

public delegate void OnUnitInit(Unit unit, UnitInit unitInit);
public delegate void OnUnitUpdate(Unit unit, UnitUpdate unitUpdate);
public delegate void OnUnitMove(Unit unit, ulong time, ZoneTransform transform);
public delegate void OnTeamEffectAdded(Unit unit, ConstEffectInfo effectInfo);
public delegate void OnTeamEffectRemoved(Unit unit, ConstEffectInfo effectInfo);
public delegate bool OnApplyInstEffect(Unit source, IEnumerable<Unit> affectedUnits, InstEffect effect, bool enqueueAction,
    ImpactData impactData, BlockShift? shift = null, Direction2D? sourceDirection = null, ResourceType? resourceType = null);
public delegate IEnumerable<ConstEffectInfo> GetTeamEffects(TeamType team);
public delegate bool DoesObjBuffApply(TeamType team, IEnumerable<UnitLabel> labels);
public delegate void OnImpactAction(Vector3 insidePoint, Vector3 shotPos, bool crit = false, Unit? sourceUnit = null,
    Key? source = null, CardImpact? card = null, IEnumerable<uint>? affectedUnits = null, Vector3s? normal = null);
public delegate float GetResourceCap();
public delegate void UpdateMatchStats(Unit player, int? kills = null, int? deaths = null, int? assists = null);

public record UnitUpdater(OnUnitInit OnUnitInit, 
    OnUnitUpdate OnUnitUpdate, 
    OnUnitMove OnUnitMove,
    OnTeamEffectAdded OnTeamEffectAdded,
    OnTeamEffectRemoved OnTeamEffectRemoved,
    OnApplyInstEffect OnApplyInstEffect,
    GetTeamEffects GetTeamEffects,
    DoesObjBuffApply DoesObjBuffApply,
    OnImpactAction OnImpactOccur,
    GetResourceCap GetResourceCap,
    UpdateMatchStats UpdateMatchStats);