using System.Numerics;
using System.Runtime.Serialization;
using BNLReloadedServer.BaseTypes;
using Octree;

namespace BNLReloadedServer.Octree_Extensions;

public readonly struct BoundingSphere(Vector3 center, float radius) : IBoundingShape
{
    public float Radius => radius;
    private readonly float _radiusSquared = MathF.Pow(radius, 2);
    public Vector3 Center { get; } = center;
    public bool Contains(Vector3 point) => Vector3.DistanceSquared(point, Center) <= _radiusSquared;

    public bool Intersects(BoundingBox box) => Contains(Vector3.Clamp(Center, box.Min, box.Max));
    
    public bool Intersects(Vector3 min, Vector3 max) => Contains(Vector3.Clamp(Center, min, max));

    public (Vector3s max, Vector3s min) GetSquareBounds() => 
        ((Vector3s)(Center + new Vector3(radius)), (Vector3s)(Center - new Vector3(radius)));
}