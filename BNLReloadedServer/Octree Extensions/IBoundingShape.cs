using System.Numerics;
using BNLReloadedServer.BaseTypes;
using Octree;

namespace BNLReloadedServer.Octree_Extensions;

public interface IBoundingShape
{
    public Vector3 Center { get; }
    public bool Contains(Vector3 point);
    public bool Intersects(BoundingBox box);
    public bool Intersects(Vector3 min, Vector3 max);
    public (Vector3s max, Vector3s min) GetSquareBounds();
    public IBoundingShape GetShapeAtNewPosition(Vector3 position);
}