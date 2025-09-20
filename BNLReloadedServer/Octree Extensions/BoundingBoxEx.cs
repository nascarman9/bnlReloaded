using System.Numerics;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.ProtocolHelpers;
using Octree;

namespace BNLReloadedServer.Octree_Extensions;

public readonly struct BoundingBoxEx : IBoundingShape
{
    private readonly Vector3 _min;

    private readonly Vector3 _max;

    public BoundingBoxEx(Vector3 center, Vector3 size)
    {
        var extents = size * 0.5f;
        Center = center;
        _min = center - extents;
        _max = center + extents;
    }

    public BoundingBoxEx(Vector3s point)
    {
        var pointF = point.ToVector3();
        _min = pointF + UnitSizeHelper.HalfImprecisionVector;
        _max = pointF + Vector3.One - UnitSizeHelper.HalfImprecisionVector;
    }

    public Vector3 Center { get; }

    public bool Contains(Vector3 point) => _min.X <= (double)point.X && _max.X >= (double)point.X &&
                                           _min.Y <= (double)point.Y && _max.Y >= (double)point.Y &&
                                           _min.Z <= (double)point.Z && _max.Z >= (double)point.Z;

    public bool Intersects(BoundingBox box) => _min.X <= (double)box.Max.X && _max.X >= (double)box.Min.X &&
                                               _min.Y <= (double)box.Max.Y && _max.Y >= (double)box.Min.Y &&
                                               _min.Z <= (double)box.Max.Z && _max.Z >= (double)box.Min.Z;

    public bool Intersects(Vector3 min, Vector3 max) => _min.X <= (double)max.X && _max.X >= (double)min.X &&
                                                        _min.Y <= (double)max.Y && _max.Y >= (double)min.Y &&
                                                        _min.Z <= (double)max.Z && _max.Z >= (double)min.Z;

    public (Vector3s max, Vector3s min) GetSquareBounds() => ((Vector3s) _max, (Vector3s) _min);
}