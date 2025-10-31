using System.Numerics;
using BNLReloadedServer.Octree_Extensions;

namespace BNLReloadedServer.BaseTypes;

public class EffectArea(IBoundingShape shape)
{
    public IBoundingShape Shape { get; private set; } = shape;
    public Unit[] NearbyUnits { get; set; } = [];

    public void AreaMoved(Vector3 location) => Shape = Shape.GetShapeAtNewPosition(location);
}