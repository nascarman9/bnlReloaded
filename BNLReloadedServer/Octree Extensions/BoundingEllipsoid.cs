using System.Numerics;
using BNLReloadedServer.BaseTypes;
using Octree;

namespace BNLReloadedServer.Octree_Extensions;

public readonly struct BoundingEllipsoid(Vector3 center, float xRadius, float yRadius, float zRadius) : IBoundingShape
{
    public BoundingEllipsoid(Vector3 center, float horizontalRadius, float verticalRadius) : 
        this(center, horizontalRadius, verticalRadius, horizontalRadius)
    {
    }
    
    public float XRadius => xRadius;
    public float YRadius => yRadius;
    public float ZRadius => zRadius;
    
    private readonly Vector3 _vecRadiusSquared = new(MathF.Pow(xRadius, 2), MathF.Pow(yRadius, 2), MathF.Pow(zRadius, 2));

    public Vector3 Center { get; } = center;

    public bool Contains(Vector3 point)
    {
        var tempVec = point - Center;
        var resVec = tempVec * tempVec / _vecRadiusSquared; 
        return resVec.X + resVec.Y + resVec.Z <= 1;
    }

    public bool Intersects(BoundingBox box) => Contains(Vector3.Clamp(Center, box.Min, box.Max));
    
    public bool Intersects(Vector3 min, Vector3 max) => Contains(Vector3.Clamp(Center, min, max));

    public (Vector3s max, Vector3s min) GetSquareBounds() => 
        ((Vector3s)(Center + new Vector3(xRadius, yRadius, zRadius)), (Vector3s)(Center - new Vector3(xRadius, yRadius, zRadius)));
}