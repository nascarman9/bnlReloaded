using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ServerTypes;

public record ShotInfo(ulong ShotId, Unit Caster, Vector3 ShotPos, Vector3? TargetPos = null, GearData? SourceGear = null, byte? ToolIndex = null, Key? SourceAbility = null)
{
}