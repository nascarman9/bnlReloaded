using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class UnitSizeHelper
{
    public static bool IsInsideUnit(Vector3s pos, Unit unit)
    {
        return unit.PlayerId.HasValue ? IsInsidePlayerUnit(pos, unit.Transform.Position, unit.Transform.IsCrouch) : IsInsideCommonUnit(pos, unit.Transform.Position, unit.UnitCard.Size);
    }

    private static bool IsInsidePlayerUnit(Vector3s pos, Vector3 unitPos, bool isCrouch)
    {
        var vector3s = (Vector3s) unitPos;
        var num1 = !isCrouch ? 1.9f : 0.9f;
        var y = vector3s.y;
        var num2 = (int) Math.Floor(unitPos.Y + num1);
        return pos.x == vector3s.x && pos.z == vector3s.z && pos.y >= y && pos.y <= num2;
    }

    private static bool IsInsideCommonUnit(Vector3s pos, Vector3 unitPos, Vector3s? unitSize)
    {
        if (!unitSize.HasValue || unitSize.Value.x == 0 || unitSize.Value.y == 0 || unitSize.Value.z == 0)
            return false;
        var vector3s1 = (Vector3s) unitPos;
        var vector3s2 = unitSize.Value;
        return pos.x >= vector3s1.x && pos.x < vector3s1.x + vector3s2.x && pos.y >= vector3s1.y && pos.y < vector3s1.y + vector3s2.y && pos.z >= vector3s1.z && pos.z < vector3s1.z + vector3s2.z;
    }
}