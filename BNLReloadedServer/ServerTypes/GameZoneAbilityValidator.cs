using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ServerTypes;

public partial class GameZone
{
    public bool ValidateAbility(Unit unit)
    {
        var abilityCard = unit.AbilityCard;
        if (abilityCard == null)
            return false;
        return abilityCard.Validate is not { Type: AbilityValidateType.Devices } ||
               ValidateAbilityDevices(unit, abilityCard.Validate as AbilityValidateDevices);
    }

    private bool ValidateAbilityDevices(Unit unit, AbilityValidateDevices? v) =>
        v == null || _units.Values.Count(target =>
            ValidateRange(unit, target, v) && ValidateBuffDisabled(target) &&
            (v.DeviceTargeting is null || Match(v.DeviceTargeting, unit, target))) >= v.MinCount;

    private static bool ValidateRange(Unit unit, Unit target, AbilityValidateDevices v) => !v.Range.HasValue ||
        Vector3.DistanceSquared(target.Transform.Position, unit.Transform.Position) <= v.Range.Value * v.Range.Value;

    private static bool ValidateBuffDisabled(Unit target) =>
        !target.IsBuff(BuffType.Disabled);

    public static int? AbilityCharges(Unit player)
    {
        int? nullable = null;
        if (player.AbilityCharges > 1)
            nullable = player.AbilityCharges;
        return nullable;
    }
    
    public static bool Match(EffectTargeting t, Unit caster, Unit target) =>
        ValidateAffectedTeam(t, caster, target) && ValidateOwnedOnly(t, caster, target) &&
        ValidateAffectedUnits(t, target) && ValidateAffectedLabels(t, target) &&
        ValidateIgnoreCaster(t, caster, target);

    private static bool ValidateAffectedTeam(EffectTargeting t, Unit caster, Unit target) =>
        t.AffectedTeam switch
        {
            RelativeTeamType.Friendly => target.Team == caster.Team,
            RelativeTeamType.Opponent => target.Team != caster.Team,
            _ => true
        };

    private static bool ValidateOwnedOnly(EffectTargeting t, Unit caster, Unit target)
    {
        if (!t.CasterOwnedOnly)
            return true;
        var ownerPlayerId = target.OwnerPlayerId;
        var valueOrDefault1 = ownerPlayerId.GetValueOrDefault();
        var playerId = caster.PlayerId;
        var valueOrDefault2 = playerId.GetValueOrDefault();
        return valueOrDefault1 == valueOrDefault2 && ownerPlayerId.HasValue == playerId.HasValue;
    }

    private static bool ValidateAffectedUnits(EffectTargeting t, Unit target) => t.AffectedUnits == null ||
        (target.UnitCard?.Data is not null && t.AffectedUnits.Contains(target.UnitCard.Data.Type));

    private static bool ValidateAffectedLabels(EffectTargeting t, Unit target)
    {
        if (t.AffectedLabels == null)
            return true;
        var labels = target.UnitCard?.Labels;
        return labels?.Any(label => t.AffectedLabels.Contains(label)) ?? false;
    }

    private static bool ValidateIgnoreCaster(EffectTargeting t, Unit caster, Unit target) =>
        !t.IgnoreCaster || caster.Id != target.Id;
}