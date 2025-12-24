using System.Numerics;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;

namespace BNLReloadedServer.ProtocolHelpers;

public static class UnitSizeHelper
{
    public static Vector3 ImprecisionVector { get; } = new(0.015f, 0.015f, 0.015f);
    public static Vector3 HalfImprecisionVector { get; } = ImprecisionVector / 2;
    
    public static bool IsInsideUnit(Vector3s pos, Unit unit) => 
        unit.PlayerId.HasValue
            ? IsInsidePlayerUnit(pos, unit.Transform.Position, unit.Transform.IsCrouch)
            : IsInsideCommonUnit(pos, unit.Transform.Position, unit.UnitCard?.Size, unit.UnitCard?.PivotType ?? UnitPivotType.Zero);
    
    public static (Vector3 max, Vector3 min) GetExactUnitBounds(Unit unit, bool withSize = false) =>
        unit.PlayerId.HasValue
            ? ExactPlayerUnitBounds(unit.GetMidpoint(), unit.Transform.IsCrouch, withSize)
            : ExactCommonUnitBounds(unit.GetMidpoint(), unit.UnitCard?.Size);

    public static (Vector3s max, Vector3s min) GetUnitBounds(Unit unit, bool withSize = false) =>
        unit.PlayerId.HasValue
            ? PlayerUnitBounds(unit.GetMidpoint(), unit.Transform.IsCrouch, withSize)
            : CommonUnitBounds(unit.GetMidpoint(), unit.UnitCard?.Size);
    
    public static (Vector3s max, Vector3s min) GetUnitBounds(Unit unit, Vector3 position, bool withSize) =>
        unit.PlayerId.HasValue
            ? PlayerUnitBounds(position, unit.Transform.IsCrouch, withSize)
            : CommonUnitBounds(position, unit.UnitCard?.Size);
    
    public static IEnumerable<(Vector3s max, Vector3s min)> GetUnitBounds(Unit unit, uint stepCount, bool withSize = false, bool withExtraStep = false) =>
        unit.PlayerId.HasValue
            ? PlayerUnitBounds(unit, unit.Transform.IsCrouch, stepCount, withSize, withExtraStep)
            : CommonUnitBounds(unit, unit.UnitCard?.Size, stepCount);

    private static bool IsInsidePlayerUnit(Vector3s pos, Vector3 unitPos, bool isCrouch)
    {
        var vector3s = (Vector3s) unitPos;
        var num1 = !isCrouch ? 1.9f : 0.9f;
        var y = vector3s.y;
        var num2 = (int) Math.Floor(unitPos.Y + num1);
        return pos.x == vector3s.x && pos.z == vector3s.z && pos.y >= y && pos.y <= num2;
    }

    private static (Vector3s max, Vector3s min) PlayerUnitBounds(Vector3 unitPos, bool isCrouch, bool withSize)
    {
        var height = !isCrouch ? 1.9f : 0.9f;
        var halfHeight = height * 0.5f - HalfImprecisionVector.Y;
        var width = withSize ? 0.25f - HalfImprecisionVector.X : 0.0f;
        var zeroCorner = unitPos - new Vector3(width, halfHeight, width);
        var maxCorner = unitPos + new Vector3(width, halfHeight, width);
        
        return ((Vector3s)maxCorner, (Vector3s)zeroCorner);
    }

    private static IEnumerable<(Vector3s max, Vector3s min)> PlayerUnitBounds(Unit player, bool isCrouch, uint stepCount, bool withSize, bool withExtraStep)
    {
        var height = !isCrouch ? 1.9f : 0.9f;
        var halfHeight = height * 0.5f - HalfImprecisionVector.Y;
        var width =  withSize ? 0.4f : 0.0f;
        foreach (var pos in player.GetPositionSteps(stepCount, withExtraStep))
        {
            var midPos = player.GetMidpoint(pos);
            var zeroCorner = midPos - new Vector3(width, halfHeight, width);
            var maxCorner = midPos + new Vector3(width, halfHeight, width);
            
            yield return ((Vector3s)maxCorner, (Vector3s)zeroCorner);
        }
    }
    
    private static (Vector3 max, Vector3 min) ExactPlayerUnitBounds(Vector3 unitPos, bool isCrouch, bool withSize)
    {
        var height = !isCrouch ? 1.9f : 0.9f;
        var halfHeight = height * 0.5f - HalfImprecisionVector.Y;
        var width = withSize ? 0.25f - HalfImprecisionVector.X : 0.0f;
        var zeroCorner = unitPos - new Vector3(width, halfHeight, width);
        var maxCorner = unitPos + new Vector3(width, halfHeight, width);
        
        return (maxCorner, zeroCorner);
    }

    private static bool IsInsideCommonUnit(Vector3s pos, Vector3 unitPos, Vector3s? unitSize, UnitPivotType pivotType)
    {
        if (!unitSize.HasValue || unitSize.Value.x == 0 || unitSize.Value.y == 0 || unitSize.Value.z == 0)
            return false;
        var vector1 = pivotType switch
        {
            UnitPivotType.Center => unitPos - unitSize.Value.ToVector3() / 2,
            UnitPivotType.CenterBottom or
            UnitPivotType.PointBottom => unitPos - unitSize.Value.ToVector3() with { Y = 0 } / 2,
            _ => unitPos
        };
        var vector2 = unitSize.Value;
        return pos.x >= vector1.X && pos.x < vector1.X + vector2.x && pos.y >= vector1.Y && pos.y < vector1.Y + vector2.y && pos.z >= vector1.Z && pos.z < vector1.Z + vector2.z;
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
    
    private static IEnumerable<(Vector3s max, Vector3s min)> CommonUnitBounds(Unit unit, Vector3s? unitSize, uint stepCount)
    {
        if (!unitSize.HasValue || unitSize.Value.x == 0 || unitSize.Value.y == 0 || unitSize.Value.z == 0)
        {
            yield return ((Vector3s)unit.Transform.Position - Vector3s.One, (Vector3s)unit.Transform.Position);
            yield break;
        }

        var half = unitSize.Value.ToVector3() * 0.5f;

        foreach (var pos in unit.GetPositionSteps(stepCount))
        {
            var midPos = unit.GetMidpoint(pos);
            var zeroCorner = midPos - half;
            var maxCorner = midPos + half;
        
            yield return ((Vector3s)(maxCorner - HalfImprecisionVector), (Vector3s)(zeroCorner + HalfImprecisionVector));
        }
    }
    
    private static (Vector3 max, Vector3 min) ExactCommonUnitBounds(Vector3 unitPos, Vector3s? unitSize)
    {
        if (!unitSize.HasValue || unitSize.Value.x == 0 || unitSize.Value.y == 0 || unitSize.Value.z == 0)
            return (unitPos, unitPos);

        var half = unitSize.Value.ToVector3() * 0.5f;
        
        var zeroCorner = unitPos - half;
        var maxCorner = unitPos + half;
        
        return (maxCorner - HalfImprecisionVector, zeroCorner + HalfImprecisionVector);
    }
}