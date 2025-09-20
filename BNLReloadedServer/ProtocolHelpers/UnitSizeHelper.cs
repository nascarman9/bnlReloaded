using System.Numerics;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;

namespace BNLReloadedServer.ProtocolHelpers;

public static class UnitSizeHelper
{
    public static Vector3 ImprecisionVector { get; } = new(0.01f, 0.01f, 0.01f);
    public static Vector3 HalfImprecisionVector { get; } = ImprecisionVector / 2;
    
    public static bool IsInsideUnit(Vector3s pos, Unit unit) => 
        unit.PlayerId.HasValue
            ? IsInsidePlayerUnit(pos, unit.Transform.Position, unit.Transform.IsCrouch)
            : IsInsideCommonUnit(pos, unit.Transform.Position, unit.UnitCard?.Size);

    public static (Vector3s max, Vector3s min) GetUnitBounds(Unit unit) =>
        unit.PlayerId.HasValue
            ? PlayerUnitBounds(unit.Transform.Position, unit.Transform.IsCrouch)
            : CommonUnitBounds(unit.Transform.Position, unit.UnitCard?.Size);

    private static bool IsInsidePlayerUnit(Vector3s pos, Vector3 unitPos, bool isCrouch)
    {
        var vector3s = (Vector3s) unitPos;
        var num1 = !isCrouch ? 1.9f : 0.9f;
        var y = vector3s.y;
        var num2 = (int) Math.Floor(unitPos.Y + num1);
        return pos.x == vector3s.x && pos.z == vector3s.z && pos.y >= y && pos.y <= num2;
    }

    private static (Vector3s max, Vector3s min) PlayerUnitBounds(Vector3 unitPos, bool isCrouch)
    {
        var height = !isCrouch ? 1.9f : 0.9f;
        var halfHeight = height * 0.5f;
        var zeroCorner = unitPos - new Vector3(0.0f, halfHeight, 0.0f);
        var maxCorner = unitPos + new Vector3(0.0f, halfHeight, 0.0f);
        
        return ((Vector3s)maxCorner, (Vector3s)zeroCorner);
    }

    private static bool IsInsideCommonUnit(Vector3s pos, Vector3 unitPos, Vector3s? unitSize)
    {
        if (!unitSize.HasValue || unitSize.Value.x == 0 || unitSize.Value.y == 0 || unitSize.Value.z == 0)
            return false;
        var vector3s1 = (Vector3s) unitPos;
        var vector3s2 = unitSize.Value;
        return pos.x >= vector3s1.x && pos.x < vector3s1.x + vector3s2.x && pos.y >= vector3s1.y && pos.y < vector3s1.y + vector3s2.y && pos.z >= vector3s1.z && pos.z < vector3s1.z + vector3s2.z;
    }

    private static (Vector3s max, Vector3s min) CommonUnitBounds(Vector3 unitPos, Vector3s? unitSize)
    {
        if (!unitSize.HasValue || unitSize.Value.x == 0 || unitSize.Value.y == 0 || unitSize.Value.z == 0)
            return ((Vector3s)unitPos - Vector3s.One, (Vector3s)unitPos);

        var half = unitSize.Value.ToVector3() * 0.5f;
        
        var zeroCorner = unitPos - half;
        var maxCorner = unitPos + half;
        
        return ((Vector3s)(maxCorner - HalfImprecisionVector), (Vector3s)(zeroCorner + HalfImprecisionVector));
    }
}