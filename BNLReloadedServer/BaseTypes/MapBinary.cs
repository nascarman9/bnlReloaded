using System.Numerics;
using BNLReloadedServer.Database;
using BNLReloadedServer.Octree_Extensions;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ServerTypes;

namespace BNLReloadedServer.BaseTypes;

internal readonly ref struct StabilityBinary(Span<byte> span, Vector3s pos)
{
    private readonly Span<byte> _span = span;
    internal const int Size = 4;
    
    public ushort StableDistance
    {
        get => BitConverter.ToUInt16(_span[..2]);
        set => BitConverter.TryWriteBytes(_span[..2], value);
    }

    public StableDirection StablePosition
    {
        get => (StableDirection)BitConverter.ToUInt16(_span[2..4]);
        set => BitConverter.TryWriteBytes(_span[2..4], (ushort)value);
    }

    public uint Int
    {
        get => BitConverter.ToUInt32(_span);
        set => BitConverter.TryWriteBytes(_span, value);
    } 
    
    public Vector3s StableVector => pos + StablePosition.ToVector();
}

internal readonly struct SplashDamagePropagation(Vector3s pos, bool[] dirCheck)
{
    public Vector3s Position { get; } = pos;
    public bool[] CanGoDir { get; } = dirCheck;
}

public record MapUpdater(Action<uint, float> OnCut, Action<uint, Key> OnMined, Action<Unit> OnDetached, Func<Action, bool> EnqueueAction);

public class MapBinary
{
    private readonly MapUpdater _mapUpdater;
    
    private readonly byte[] _data;

    private readonly byte[] _stabilityData;

    private float _liquidPlane;
    
    public readonly Dictionary<Vector3s, Unit> OwnedBlocks = new();
    
    public readonly Dictionary<Vector3s, Unit?[]> AttachedUnits = new();

    public readonly Dictionary<Vector3s, BlockIntervalUpdater> UnitsInsideBlock = new();
    
    public BoundsOctreeEx<Unit>? Units { get; set; }
    
    private const ushort NormalDest = 1;
    private const ushort FallingDest = 2;
    private const ushort SplashDest = 3;

    private const float NaturalFalloff = 0.05f;

    public MapBinary(byte[] binary, float liquidPlane, MapUpdater mapUpdater)
    {
        _mapUpdater = mapUpdater;
        var binaryReader = new BinaryReader(binary.UnZip());
        SizeX = binaryReader.ReadUInt16();
        SizeY = binaryReader.ReadUInt16();
        SizeZ = binaryReader.ReadUInt16();
        var count = SizeX * SizeY * SizeZ * 6;
        _data = new byte[count];
        if (binaryReader.Read(_data, 0, count) != count)
            throw new EndOfStreamException();
        
        var stableCount = SizeX * SizeY * SizeZ * StabilityBinary.Size;
        _stabilityData = new byte[stableCount];
        InitStabilityData(liquidPlane);
    }

    public MapBinary(int schema, byte[] binary, Vector3s size, float liquidPlane, MapUpdater mapUpdater)
    {
        _mapUpdater = mapUpdater;
        var input = binary.UnZip();
        var binaryReader = new BinaryReader(input);
        SizeX = size.x;
        SizeY = size.y;
        SizeZ = size.z;
        var count = SizeX * SizeY * SizeZ * 6;
        _data = new byte[count];
        if (SizeX * SizeY * SizeZ * 4 == input.Length)
        {
            for (var index = 0; index < SizeX * SizeY * SizeZ; ++index)
            {
                var bytes1 = BitConverter.GetBytes((ushort)binaryReader.ReadByte());
                _data[index * 6] = bytes1[0];
                _data[index * 6 + 1] = bytes1[1];
                _data[index * 6 + 2] = binaryReader.ReadByte();
                var bytes2 = BitConverter.GetBytes((ushort)binaryReader.ReadByte());
                _data[index * 6 + 3] = bytes2[0];
                _data[index * 6 + 4] = bytes2[1];
                _data[index * 6 + 5] = binaryReader.ReadByte();
            }
        }
        else if (SizeX * SizeY * SizeZ * 5 == input.Length)
        {
            for (var index = 0; index < SizeX * SizeY * SizeZ; ++index)
            {
                var bytes3 = BitConverter.GetBytes(binaryReader.ReadUInt16());
                _data[index * 6] = bytes3[0];
                _data[index * 6 + 1] = bytes3[1];
                _data[index * 6 + 2] = binaryReader.ReadByte();
                var bytes4 = BitConverter.GetBytes((ushort)binaryReader.ReadByte());
                _data[index * 6 + 3] = bytes4[0];
                _data[index * 6 + 4] = bytes4[1];
                _data[index * 6 + 5] = binaryReader.ReadByte();
            }
        }
        else if (binaryReader.Read(_data, 0, count) != count)
            throw new EndOfStreamException();
        
        var stableCount = SizeX * SizeY * SizeZ * StabilityBinary.Size;
        _stabilityData = new byte[stableCount];
        InitStabilityData(liquidPlane);
    }

    public int SizeX { get; }

    public int SizeY { get; }

    public int SizeZ { get; }

    public BlockBinary this[Vector3s pos] =>
        new(_data.AsSpan(((pos.x * SizeY + pos.y) * SizeZ + pos.z) * BlockBinary.Size, BlockBinary.Size), pos);

    public BlockBinary this[int x, int y, int z] => this[new Vector3s(x, y, z)];

    private StabilityBinary StableData(int x, int y, int z) => StableData(new Vector3s(x, y, z));
    
    private StabilityBinary StableData(Vector3s pos) => 
        new(_stabilityData.AsSpan(((pos.x * SizeY + pos.y) * SizeZ + pos.z) * StabilityBinary.Size, StabilityBinary.Size),
        pos);
        
    public Vector3s Size => new(SizeX, SizeY, SizeZ);

    private void InitStabilityData(float liquidPlane)
    {
        _liquidPlane = liquidPlane;
        var blockQueue = new Queue<(Vector3s, ushort)>();
        var visitedBlocks = new HashSet<Vector3s>();
        for (short x = 0; x < SizeX; x++)
        {
            for (short y = 0; y < SizeY; y++)
            {
                for (short z = 0; z < SizeZ; z++)
                {
                    var stable = StableData(x, y, z);
                    var block = this[x, y, z];
                    
                    if (block.IsAir || block.IsLocked)
                    {
                        stable.Int = uint.MaxValue;
                    }
                    else
                    {
                        if (block.Card.CanFloat || block.Y == (short)liquidPlane)
                        {
                            stable.StableDistance = 0;
                            blockQueue.Enqueue((block.Position, 0));
                            visitedBlocks.Add(block.Position);
                        }
                        else if (block.Y < (short)liquidPlane)
                        {
                            stable.StableDistance = 0;
                        }
                        else
                        {
                            stable.StableDistance = ushort.MaxValue;
                        }
                        
                        stable.StablePosition = StableDirection.Inherent;
                    }
                }
            }
        }
        
        while (blockQueue.TryDequeue(out var point))
        {
            var (pos, dist) = point;
            
            foreach (var b in GetBorderingFaces(pos,
                         p => !visitedBlocks.Contains(p) && CheckIfStable(pos, dist)(p)))
            {
                var distance = (ushort)(dist + 1);
                var stb = StableData(b);
                
                stb.StableDistance = distance;
                stb.StablePosition = b.ToStableDirection(pos);
                
                blockQueue.Enqueue((b, distance));
                visitedBlocks.Add(b);
            }
        }
    }

    public BlockArrayMap3D ToMap3D()
    {
        var map3D = new BlockArrayMap3D(Size);
        map3D.Change((ref value, ref pos) => value = this[pos].ToBlock());
        return map3D;
    }

    public BlockArrayMap3D ToMap3D(byte[] colors)
    {
        var map3D = ToMap3D();
        DecodeColors(map3D, colors);
        return map3D;
    }

    public byte[] ToBinary()
    {
        var output = new MemoryStream();
        var binaryWriter = new BinaryWriter(output);
        binaryWriter.Write((ushort)SizeX);
        binaryWriter.Write((ushort)SizeY);
        binaryWriter.Write((ushort)SizeZ);
        binaryWriter.Write(_data);
        binaryWriter.Flush();
        return output.ToArray().Zip(3).ToArray();
    }

    public static byte[] Pack(BlockMap3D map)
    {
        var output = new MemoryStream();
        var binaryWriter = new BinaryWriter(output);
        for (var x = 0; x < map.SizeX; ++x)
        {
            for (var y = 0; y < map.SizeY; ++y)
            {
                for (var z = 0; z < map.SizeZ; ++z)
                {
                    var block = map[x, y, z];
                    binaryWriter.Write(block.Id);
                    binaryWriter.Write(block.Damage);
                    binaryWriter.Write(block.Vdata);
                    binaryWriter.Write(block.Ldata);
                }
            }
        }

        binaryWriter.Flush();
        return output.ToArray().Zip(3).ToArray();
    }

    public static void DecodeColors(BlockMap3D map, byte[]? binary)
    {
        if (binary == null)
            return;
        var binaryReader = new BinaryReader(binary.UnZip());
        for (var x = 0; x < map.SizeX; ++x)
        {
            for (var y = 0; y < map.SizeY; ++y)
            {
                for (var z = 0; z < map.SizeZ; ++z)
                {
                    var block = map[x, y, z] with
                    {
                        Color = binaryReader.ReadByte()
                    };
                    map[x, y, z] = block;
                }
            }
        }
    }

    public static byte[] EncodeColors(BlockMap3D map)
    {
        var output = new MemoryStream();
        var binaryWriter = new BinaryWriter(output);
        foreach (var block in map)
            binaryWriter.Write(block.Color);
        binaryWriter.Flush();
        return output.ToArray().Zip(3).ToArray();
    }

    private void OnBlockRemoved(Vector3s blockPos)
    {
        OwnedBlocks.Remove(blockPos);
        if (UnitsInsideBlock.TryGetValue(blockPos, out var unitsInside))
        {
            unitsInside.Clear();
        }
        
        UnitsInsideBlock.Remove(blockPos);
        if (AttachedUnits.TryGetValue(blockPos, out var units))
        {
            foreach (var unit in units.OfType<Unit>())
            {
                _mapUpdater.OnDetached(unit);
            }
        }
        AttachedUnits.Remove(blockPos);
    }

    // Assumes position has stability
    private void PropagateStability(Vector3s position, ushort startDistance = 0)
    {
        var blockQueue = new Queue<(Vector3s, ushort)>();
        var visitedBlocks = new HashSet<Vector3s>();
        blockQueue.Enqueue((position, startDistance));
        visitedBlocks.Add(position);
        
        while (blockQueue.TryDequeue(out var point))
        {
            var (pos, dist) = point;
            
            foreach (var b in GetBorderingFaces(pos,
                         p => !visitedBlocks.Contains(p) && CheckIfStable(pos, dist)(p)))
            {
                var distance = (ushort)(dist + 1);
                var stb = StableData(b);
                
                stb.StableDistance = distance;
                stb.StablePosition = b.ToStableDirection(pos);
                
                blockQueue.Enqueue((b, distance));
                visitedBlocks.Add(b);
            }
        }
    }

    // Assumes position has no stability
    private (Dictionary<Vector3s, BlockUpdate> updates, float totalResources) PropagateInstability(Vector3s position)
    {
        var possiblyUnstable = GetBordering(position, p => position == StableData(p).StableVector).ToList();
        var dict = new Dictionary<Vector3s, BlockUpdate>();
        var totalRes = 0.0f;
        if (possiblyUnstable.Count == 0) return (dict, totalRes);
        
        var blockQueue = new Queue<Vector3s>();
        var propQueue = new PriorityQueue<Vector3s, ushort>();
        var visitedBlocks = new HashSet<Vector3s>();
        var propBlocks = new HashSet<Vector3s>();
        
        blockQueue.Enqueue(position);

        while (blockQueue.TryDequeue(out var point))
        {
            var pos = point;
            foreach (var b in GetBorderingFaces(pos,
                         p => !visitedBlocks.Contains(p) && !propBlocks.Contains(p) && CheckIfStable(pos)(p)))
            {
                var stb = StableData(b);
                if (visitedBlocks.Contains(stb.StableVector) || stb.StableVector == position)
                {
                    visitedBlocks.Add(b);
                    stb.StableDistance = ushort.MaxValue;
                    blockQueue.Enqueue(b);
                }
                else
                {
                    propBlocks.Add(b);
                    propQueue.Enqueue(b, stb.StableDistance);
                }
            }
        }
        
        while (propQueue.TryDequeue(out var propPoint, out var distance))
        {
            var pos = propPoint;
            var dist = distance;
            foreach (var b in GetBorderingFaces(pos,
                         p => !propBlocks.Contains(p) && CheckIfStable(pos, dist)(p)))
            {
                var stb = StableData(b);
                stb.StableDistance = (ushort)(dist + 1);
                stb.StablePosition = b.ToStableDirection(pos);
                propBlocks.Add(b);
                visitedBlocks.Remove(b);
                propQueue.Enqueue(b, stb.StableDistance);
            }
        }
        
        foreach (var b in visitedBlocks)
        {
            var stb = StableData(b);
            stb.Int = uint.MaxValue;
            var block = this[b];
            totalRes += block.Card.Reward?.PlayerReward ?? 0;

            block.Id = 0;
            block.Damage = 0;
            block.VData = 0;
            block.Team = TeamType.Neutral;

            dict[b] = block.ToUpdate(FallingDest);
            OnBlockRemoved(b);
        }

        return (dict, totalRes);
    }

    private void UpdateStability(Vector3s position)
    {
        var block = this[position];
        var data = StableData(position);
        if (block.Card.CanFloat)
        {
            data.StableDistance = 0;
            data.StablePosition = StableDirection.Inherent;
            PropagateStability(position);
            return;
        }

        data.StablePosition = StableDirection.Inherent;
        data.StableDistance = ushort.MaxValue;
        var validBlocks = GetBorderingFaces(position, CheckIfStable(position));
        foreach (var pos in validBlocks.OrderBy(p => StableData(p).StableDistance))
        {
            var stb = StableData(pos);
            if (stb.StableDistance > data.StableDistance + 1)
            {
                PropagateStability(position, data.StableDistance);
                break;
            }

            if (stb.StableDistance + 1 >= data.StableDistance) continue;
            data.StableDistance = (ushort)(stb.StableDistance + 1);
            data.StablePosition = position.ToStableDirection(pos);
        }
    }

    public bool ContainsBlock(Vector3s pos) => pos.x >= 0 && pos.x < SizeX && pos.y >= 0 && pos.y < SizeY && pos.z >= 0 && pos.z < SizeZ;

    private IEnumerable<Vector3s> GetBordering(Vector3s pos, Func<Vector3s, bool>? filter)
    {
        var blocks = CoordsHelper.FaceToVector.Select(v => pos + v);
        return filter == null ? blocks.Where(ContainsBlock) : blocks.Where(point => ContainsBlock(point) && filter(point));
    }
    
    private IEnumerable<Vector3s> GetBorderingFaces(Vector3s pos, Func<Vector3s, bool>? filter)
    {
        var blocks = GetValidFaces(pos);
        return filter == null ? blocks.Where(ContainsBlock) : blocks.Where(point => ContainsBlock(point) && filter(point));
    }

    private Func<Vector3s, bool> CheckIfStable(Vector3s pos, ushort? distance = null, bool ignoreSlope = false)
    {
        if (distance == null)
        {
            return p =>
            {
                var stb = StableData(p);
                var blk = this[p];
                var blkCard = blk.Card;

                if (blkCard.Grounded && CoordsHelper.VectorToFace(pos - p) != BlockFace.Bottom)
                {
                    return false;
                }
                
                if (blkCard.IsVisualSlope && !ignoreSlope)
                {
                    var attachedFace = CoordsHelper.VectorToFace(pos - p);
                    if (SlopeBuilder.SidesCorners[(int)attachedFace]
                            .Count(c => SlopeBuilder.IsCorner(c, (byte)this[p].VData)) < 3)
                    {
                        return false;
                    }
                        
                }
                else if (blkCard.IsVisualPrefab)
                {
                    var attachedFace = CoordsHelper.VectorToFace(pos - p);
                    if (!PrefabBuilder.IsSolidFace(blk, attachedFace))
                    {
                        return false;
                    }
                }

                return stb.Int != uint.MaxValue;
            };
        }

        return p =>
        {
            var stb = StableData(p);
            var blk = this[p];
            var blkCard = blk.Card;

            if (blkCard.Grounded && CoordsHelper.VectorToFace(pos - p) != BlockFace.Bottom)
            {
                return false;
            }
            
            if (blkCard.IsVisualSlope)
            {
                var attachedFace = CoordsHelper.VectorToFace(pos - p);
                if (SlopeBuilder.SidesCorners[(int)attachedFace]
                        .Count(c => SlopeBuilder.IsCorner(c, (byte)this[p].VData)) < 3)
                {
                    return false;
                }
            }
            else if (blkCard.IsVisualPrefab)
            {
                var attachedFace = CoordsHelper.VectorToFace(pos - p);
                if (!PrefabBuilder.IsSolidFace(blk, attachedFace))
                {
                    return false;
                }
            }

            return stb.Int != uint.MaxValue && stb.StableDistance > distance.Value + 1;
        };
    }

    public IEnumerable<Vector3s> GetValidFaces(Vector3s pos, bool buildCheck = false)
    {
        var faces = CoordsHelper.OppositeFace;
        var blk = this[pos];
        var blkCard = blk.Card;
        
        if (buildCheck && (blk.IsAir || blk.IsLocked))
        {
            return [];
        }
        if (blkCard.Grounded)
        {
            return [Vector3s.Down + pos];
        }
        if (blkCard.IsVisualPrefab)
        {
            return faces.Where(f => PrefabBuilder.IsSolidFace(this[pos], f))
                .Select(fc => CoordsHelper.FaceToVector[(int)fc] + pos);
        }
        if (blkCard.IsVisualSlope && !buildCheck)
        {
            return faces
                .Where(f => SlopeBuilder.SidesCorners[(int)f]
                    .Count(c => SlopeBuilder.IsCorner(c, (byte)this[pos].VData)) >= 3)
                .Select(fc => CoordsHelper.FaceToVector[(int)fc] + pos);
        }
        
        return faces.Select(fc => CoordsHelper.FaceToVector[(int)fc] + pos);
    }
    
    public IEnumerable<BlockFace> GetValidFacesActual(Vector3s pos, bool buildCheck = false)
    {
        var faces = CoordsHelper.OppositeFace;
        var blk = this[pos];
        var blkCard = blk.Card;
        
        if (buildCheck && (blk.IsAir || blk.IsLocked))
        {
            return [];
        }
        if (blkCard.Grounded)
        {
            return [BlockFace.Bottom];
        }
        if (blkCard.IsVisualPrefab)
        {
            return faces.Where(f => PrefabBuilder.IsSolidFace(this[pos], f));
        }
        if (blkCard.IsVisualSlope && !buildCheck)
        {
            return faces
                .Where(f => SlopeBuilder.SidesCorners[(int)f]
                    .Count(c => SlopeBuilder.IsCorner(c, (byte)this[pos].VData)) >= 3);
        }
        
        return faces;
    }

    public bool GetIsActuallyInside(Unit unit, Vector3s pos)
    {
        if (!ContainsBlock(pos)) return false;
        var block = this[pos];
        var blockCard = block.Card;
        var unitMidpoint = unit.GetMidpoint();
        var floatPos = pos.ToVector3();
        var minY = floatPos.Y + UnitSizeHelper.HalfImprecisionVector.Y;
        var maxY = floatPos.Y + blockCard.Visual?.Type switch
        {
            BlockVisualType.Prefab => blockCard.Solid
                ? 1
                : 0.9f,
            BlockVisualType.CroppedCube => ContainsBlock(pos with { y = (short)(pos.y + 1) }) &&
                                           this[pos with { y = (short)(pos.y + 1) }].IsSolid
                ? 1
                : 0.9f,
            _ => 1
        } - UnitSizeHelper.HalfImprecisionVector.Y;

        var clampedY = float.Clamp(unitMidpoint.Y, minY, maxY);
        var (max, min) = UnitSizeHelper.GetExactUnitBounds(unit);
        
        return clampedY <= max.Y && clampedY >= min.Y;
    }

    public bool GetCanFit(Unit unit, Vector3 position)
    {
        var (max, min) = UnitSizeHelper.GetUnitBounds(unit, position, true);
        if (!ContainsBlock(min) && !ContainsBlock(max))
        {
            return true;
        }
        
        for (var x = Math.Clamp(min.x, 0, SizeX - 1); x <= Math.Clamp(max.x, 0, SizeX - 1); x++)
        {
            for (var y = Math.Clamp(min.y, 0, SizeY - 1); y <= Math.Clamp(max.y, 0, SizeY - 1); y++)
            {
                for (var z = Math.Clamp(min.z, 0, SizeZ - 1); z <= Math.Clamp(max.z, 0, SizeZ - 1); z++)
                {
                    if (this[new Vector3s(x, y, z)].Card.Passable != BlockPassableType.Any)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public HashSet<Vector3s> GetContainedInUnits(ICollection<Unit> units, uint stepCount = 2, bool withSize = false, bool withExtraStep = false)
    {
        var contained = new HashSet<Vector3s>();
        foreach (var unit in units)
        {
            foreach (var (max, min) in UnitSizeHelper.GetUnitBounds(unit, stepCount, withSize, withExtraStep))
            {
                if (!ContainsBlock(min) && !ContainsBlock(max))
                {
                    continue;
                }

                for (var x = Math.Clamp(min.x, 0, SizeX - 1); x <= Math.Clamp(max.x, 0, SizeX - 1); x++)
                {
                    for (var y = Math.Clamp(min.y, 0, SizeY - 1); y <= Math.Clamp(max.y, 0, SizeY - 1); y++)
                    {
                        for (var z = Math.Clamp(min.z, 0, SizeZ - 1); z <= Math.Clamp(max.z, 0, SizeZ - 1); z++)
                        {
                            contained.Add(new Vector3s(x, y, z));
                        }
                    }
                }
            }
        }
        
        return contained;
    }
    
    public HashSet<Vector3s> GetContainedInUnit(Unit unit, uint stepCount = 2, bool withSize = false, bool withExtraStep = false)
    {
        var contained = new HashSet<Vector3s>();
        foreach (var (max, min) in UnitSizeHelper.GetUnitBounds(unit, stepCount, withSize, withExtraStep))
        {
            if (!ContainsBlock(min) && !ContainsBlock(max))
            {
                continue;
            }

            for (var x = Math.Clamp(min.x, 0, SizeX - 1); x <= Math.Clamp(max.x, 0, SizeX - 1); x++)
            {
                for (var y = Math.Clamp(min.y, 0, SizeY - 1); y <= Math.Clamp(max.y, 0, SizeY - 1); y++)
                {
                    for (var z = Math.Clamp(min.z, 0, SizeZ - 1); z <= Math.Clamp(max.z, 0, SizeZ - 1); z++)
                    {
                        contained.Add(new Vector3s(x, y, z));
                    }
                }
            }
        }
        
        return contained;
    }
    
    public static (Dictionary<Vector3s, HashSet<Unit>> unitsForBlock, Dictionary<Unit, HashSet<Vector3s>> blocksForUnit)
        GetUnitBlockPositions(ICollection<Unit> units)
    {
        var unitsForBlock = new Dictionary<Vector3s, HashSet<Unit>>();
        var blocksForUnit = new Dictionary<Unit, HashSet<Vector3s>>();
        foreach (var unit in units)
        {
            foreach (var pos in unit.OverlappingMapBlocks)
            {
                if (unitsForBlock.TryGetValue(pos, out var unitSet))
                {
                    unitSet.Add(unit);
                }
                else
                {
                    unitsForBlock.Add(pos, [unit]);
                }

                if (blocksForUnit.TryGetValue(unit, out var blockSet))
                {
                    blockSet.Add(pos);
                }
                else
                {
                    blocksForUnit.Add(unit, [pos]);
                }
            }
        }

        return (unitsForBlock, blocksForUnit);
    }

    public Vector3s? CheckBlocks(IBoundingShape bounds, Func<BlockBinary, bool> check)
    {
        var startPoint = (Vector3s)bounds.Center;

        if (!ContainsBlock(startPoint))
        {
            return null;
        }
        
        var blockQueue = new Queue<Vector3s>();
        var visitedBlocks = new HashSet<Vector3s>();
        blockQueue.Enqueue(startPoint);
        visitedBlocks.Add(startPoint);
        
        while (blockQueue.TryDequeue(out var point))
        {
            var block = this[point];
            if (check(block))
            {
                return point;
            }
            
            foreach (var b in GetBordering(point,
                         p => !visitedBlocks.Contains(p) &&
                              bounds.Intersects(p.ToVector3(), (p + Vector3s.One).ToVector3())))
            {
                blockQueue.Enqueue(b);
                visitedBlocks.Add(b);
            }
        }
        
        return null;
    }

    public IEnumerable<Vector3s> EnumerateBlocks(IBoundingShape bounds, Func<BlockBinary, bool>? check)
    {
        var startPoint = (Vector3s)bounds.Center;

        if (!ContainsBlock(startPoint))
        {
            yield break;
        }
        
        var blockQueue = new Queue<Vector3s>();
        var visitedBlocks = new HashSet<Vector3s>();
        blockQueue.Enqueue(startPoint);
        visitedBlocks.Add(startPoint);
        
        while (blockQueue.TryDequeue(out var point))
        {
            var block = this[point];
            if (check is null || check(block))
            {
                yield return point;
            }
            
            foreach (var b in GetBordering(point,
                         p => !visitedBlocks.Contains(p) &&
                              bounds.Intersects(p.ToVector3(), (p + Vector3s.One).ToVector3())))
            {
                blockQueue.Enqueue(b);
                visitedBlocks.Add(b);
            }
        }
    }

    public ushort SetVData(CardBlock thisBlock, Vector3s otherBlock, BlockFace attachPoint, Direction2D placeDirection)
    {
        var attachedBlock = this[otherBlock];
        return thisBlock switch
        {
            { IsVisualClone: true } => attachedBlock.Card.IsVisualClone ? attachedBlock.VData : attachedBlock.Id,
            { IsVisualPrefab: true, Visual: not null } => PrefabBuilder.MakeData(thisBlock.Visual,
                CoordsHelper.OppositeFace[(int)attachPoint], (int)placeDirection, 0),
            _ => ushort.MinValue
        };
    }

    public Dictionary<Vector3s, BlockUpdate> AddBlocks(BlocksPattern pattern, Vector3 location, BlockShift? shift, Unit? owner)
    {
        IBoundingShape bounds;
        float chance;
        Key blockKey;
        if (!this[(Vector3s)location].IsReplaceable)
        {
            location += shift switch
            {
                BlockShift.Left => Vector3s.Left.ToVector3() * ((location.X - float.Truncate(location.X)) * 2),
                BlockShift.Right => Vector3.UnitX * ((1 - (location.X - float.Truncate(location.X))) * 2),
                BlockShift.Bottom => Vector3s.Down.ToVector3() * ((location.Y - float.Truncate(location.Y)) * 2),
                BlockShift.Top => Vector3.UnitY * ((1 - (location.Y - float.Truncate(location.Y))) * 2),
                BlockShift.Back => Vector3s.Back.ToVector3() * ((location.Z - float.Truncate(location.Z)) * 2),
                BlockShift.Front => Vector3.UnitZ * ((1 - (location.Z - float.Truncate(location.Z))) * 2),
                _ => Vector3.Zero
            };
        }
        
        switch (pattern)
        {
            case BlocksPatternOne blocksPatternOne:
                return AddBlock(blocksPatternOne.BlockKey, (Vector3s)location, CoordsHelper.GetCollidingBlock(location),
                    Direction2D.Left, owner);
            case BlocksPatternSphere blocksPatternSphere:
                bounds = new BoundingSphere(location, blocksPatternSphere.Radius);
                chance = blocksPatternSphere.FillRate;
                blockKey = blocksPatternSphere.BlockKey;
                break;
            case BlocksPatternSpit blocksPatternSpit:
                bounds = new BoundingSphere(location, blocksPatternSpit.Radius);
                chance = 0.1f;
                blockKey = blocksPatternSpit.BlockKey;
                break;
            default:
                return new Dictionary<Vector3s, BlockUpdate>();
        }
        
        var blockCard = Databases.Catalogue.GetCard<CardBlock>(blockKey);
        var dict = new Dictionary<Vector3s, BlockUpdate>();
        if (blockCard == null) return dict;
        
        var rand = new Random();
        foreach (var pos in EnumerateBlocks(bounds, CanPlaceBlock(blockCard, Units?.GetColliding(bounds) ?? [])))
        {
            var block = this[pos];
            if (rand.NextSingle() > chance) continue;

            if (blockCard.Grounded)
            {
                var bottomBlockPos = block.Position with { y = (short)(block.Y - 1) };
                var bottomBlock = this[bottomBlockPos];
                if (bottomBlock.Card.IsVisualSlope && SlopeBuilder.SidesCorners[(int)BlockFace.Top]
                        .Any(c => !SlopeBuilder.IsCorner(c, (byte)this[bottomBlockPos].VData)))
                {
                    continue;
                }
            }
            
            block.Id = blockCard.BlockId;
            block.Damage = 0;
            block.VData = 0;

            var hasTeam = block.Card.HasTeam;
            block.Team = hasTeam ? owner?.Team ?? TeamType.Neutral : TeamType.Neutral;
            if (owner is not null && hasTeam)
            {
                OwnedBlocks[block.Position] = owner;
            }
            
            UpdateStability(block.Position);
            dict[block.Position] = block.ToUpdate();
        }
        
        return dict;
    }

    public Dictionary<Vector3s, BlockUpdate> AddBlock(Key blockKey, Vector3s location, Vector3s attachTo,
        Direction2D placeDirection, Unit? owner)
    {
        var blockCard = Databases.Catalogue.GetCard<CardBlock>(blockKey);
        var dict = new Dictionary<Vector3s, BlockUpdate>();
        var collidingUnits = Units?.GetColliding(new BoundingBoxEx(location)) ?? [];
        var collidingAttachToUnits = Units?.GetColliding(new BoundingBoxEx(attachTo)) ?? [];
        if (blockCard == null || (!blockCard.CanSwim && location.y <= _liquidPlane)) return dict;
        
        if (blockCard.Grounded)
        {
            attachTo = location with { y = (short)(location.y - 1) };
        }
        
        if (!CanPlaceBlock(blockCard, collidingUnits, attachTo, collidingAttachToUnits)(this[location]))
            return dict;
        
        var block = this[location];
        var attachedBlock = this[attachTo];
        block.Id = blockCard.BlockId;
        block.Damage = 0;
        if (ContainsBlock(attachTo))
        {
            var attachedFace = CoordsHelper.VectorToFace(location - attachTo);
            block.VData = SetVData(blockCard, attachTo, attachedFace, placeDirection);
            if (attachedBlock.Card.IsVisualSlope)
            {
                foreach (var corner in SlopeBuilder.SidesCorners[(int)attachedFace])
                {
                    if (SlopeBuilder.IsCorner(corner, (byte)attachedBlock.VData)) continue;
                    attachedBlock.VData = 0;
                    dict[attachTo] = attachedBlock.ToUpdate();
                    UpdateStability(attachTo);
                    break;
                }
            }
        }
        else
        {
            block.VData = 0;
        }

        var hasTeam = block.Card.HasTeam;
        block.Team = hasTeam ? owner?.Team ?? TeamType.Neutral : TeamType.Neutral;

        if (owner is not null && hasTeam)
        {
            OwnedBlocks[location] = owner;
        }
            
        UpdateStability(location);
        dict[location] = block.ToUpdate();

        return dict;
    }

    public Dictionary<Vector3s, BlockUpdate> ReplaceBlocks(Key blockKey, float range, Vector3 location, Unit? owner)
    {
        var bounds = new BoundingSphere(location, range);
        var blockCard = Databases.Catalogue.GetCard<CardBlock>(blockKey);
        var dict = new Dictionary<Vector3s, BlockUpdate>();
        if (blockCard == null) return dict;

        foreach (var pos in EnumerateBlocks(bounds, CanReplaceBlock()))
        {
            var block = this[pos];
            block.Id = blockCard.BlockId;
            if (!blockCard.IsVisualSlope)
            {
                block.VData = 0;
            }
            
            var hasTeam = block.Card.HasTeam;
            block.Team = hasTeam ? owner?.Team ?? TeamType.Neutral : TeamType.Neutral;
            
            if (owner is not null && hasTeam)
            {
                OwnedBlocks[block.Position] = owner;
            }
            
            dict[block.Position] = block.ToUpdate();
        }
        
        return dict;
    }

    // This expects the target block to exist
    public Dictionary<Vector3s, BlockUpdate> DamageBlock(Vector3s location, DamageData damage, Unit? attacker, bool ignoreToughness = false)
    {
        var block = this[location];
        var blockCard = block.Card;
        var dict = new Dictionary<Vector3s, BlockUpdate>();
        if (block.IsAir || block.IsLocked || (!blockCard.Destructible && !damage.IgnoreInvincibility) ||
            !(blockCard.Health?.MaxHealth > 0)) return dict;

        var toughness = ignoreToughness ? 0 : blockCard.Health.Toughness;
        
        var dmgAmount = MathF.Max(damage.BlockDamage - toughness, 0) *
                        (byte.MaxValue / blockCard.Health.MaxHealth);

        if (block.Damage + dmgAmount >= byte.MaxValue)
        {
            if (attacker is { PlayerId: not null })
            {
                if (blockCard.Reward is not null && (!blockCard.Reward.Mining || damage.Mining))
                {
                    if (attacker.Team != block.Team && blockCard.Reward.EnemyReward is not null)
                    {
                        var destroyValue = Unit.ResolveDeviceResourceValue(blockCard, blockCard.Reward.EnemyReward,
                            blockCard.Reward.PlayerReward);
                        attacker.AddResource(blockCard.Reward.EnemyReward.Value, ResourceType.Mining);
                        attacker.DestroyedBlock(blockCard.DeviceType, destroyValue);
                    }
                    else if (blockCard.Reward.PlayerReward is not null)
                    {
                        var destroyValue = Unit.ResolveDeviceResourceValue(blockCard, blockCard.Reward.EnemyReward,
                            blockCard.Reward.PlayerReward);
                        attacker.AddResource(blockCard.Reward.PlayerReward.Value, ResourceType.Mining);
                        attacker.DestroyedBlock(blockCard.DeviceType, destroyValue);
                    }
                }
            }

            var blockKey = blockCard.Key;
            block.Id = 0;
            block.Damage = 0;
            block.VData = 0;
            block.Team = TeamType.Neutral;
                
            OnBlockRemoved(location);
            StableData(location).Int = uint.MaxValue;
            dict[location] = block.ToUpdate(NormalDest);
            
            if (damage.Mining && attacker is { PlayerId: not null })
            {
                _mapUpdater.OnMined(attacker.PlayerId.Value, blockKey);
            }
            
            var (cutBlocks, cutRes) = PropagateInstability(location);
            foreach (var cut in cutBlocks)
            {
                dict[cut.Key] = cut.Value;
            }

            if (cutRes > 0 && attacker is { OwnerPlayerId: not null })
            {
                _mapUpdater.OnCut(attacker.OwnerPlayerId.Value, cutRes);
            }
        }
        else
        {
            block.Damage += (byte) float.Truncate(dmgAmount);
            dict[location] = block.ToUpdate();
        }

        return dict;
    }

    public (Dictionary<Vector3s, BlockUpdate> updates, List<Unit> hitUnits) SplashDamageBlocks(Vector3[] locations,
        DamageData damage, ImpactData impact, float radius, ICollection<Unit> unitsInRadius, Unit? attacker, TeamType? attackingTeam)
    {
        var dict = new Dictionary<Vector3s, BlockUpdate>();
        var hitUnits = new List<Unit>();
        var visitedBlocks = new HashSet<Vector3s>();
        var maxTravCount = CoordsHelper.MaxBlockTraversal(radius);
        var (unitsForBlock, blocksForUnit) = GetUnitBlockPositions(unitsInRadius);
        var blockQueue = new PriorityQueue<(SplashDamagePropagation prop, uint travCount), float>();
        var naturalFalloff = Math.Min(NaturalFalloff, 1f / maxTravCount);
        
        var radiusSqrd = radius * radius;
        
        foreach (var startBlock in locations.Select(l => (Vector3s)l))
        {
            if (!ContainsBlock(startBlock)) continue;
            var startBlockData = this[startBlock];
            if (!startBlockData.Card.Destructible && !damage.IgnoreInvincibility) continue;
            
            var startDirCount = new bool[6];
            Array.Fill(startDirCount, true);
            blockQueue.Enqueue((new SplashDamagePropagation(startBlock, startDirCount), 0), 0);
        }

        while (blockQueue.TryDequeue(out var propInfo, out var dmgReduction))
        {
            if (!visitedBlocks.Add(propInfo.prop.Position))
                continue;

            var dmg = dmgReduction > 0
                ? damage.ReduceByPercent(dmgReduction - naturalFalloff * propInfo.travCount, dmgReduction)
                : damage;
            if (unitsForBlock.TryGetValue(propInfo.prop.Position, out var units))
            {
                foreach (var unit in units)
                {
                    if (!dmg.IsZeroDamage())
                    {
                        _mapUpdater.EnqueueAction(() => unit.TakeDamage(dmg, impact, true, attacker, attackingTeam));
                    }
                    hitUnits.Add(unit);
                    foreach (var block in blocksForUnit[unit])
                    {
                        if (unitsForBlock.TryGetValue(block, out var blockUnits))
                        {
                            blockUnits.Remove(unit);
                        }
                    }

                    blocksForUnit.Remove(unit);
                }
                
                unitsForBlock.Remove(propInfo.prop.Position);
            }
            
            var blk = this[propInfo.prop.Position];
            var blkCard = blk.Card;
            var dmgTaken = 0.0f;
            var checkOpenFaces = false;
            var onlyOpenFaces = false;
            var oldVdata = blk.VData;
            if (blkCard.Health?.MaxHealth > 0)
            {
                if (dmg.BlockDamage == 0)
                {
                    continue;
                }
                
                var dmgAmount = MathF.Max(dmg.BlockDamage - blkCard.Health.Toughness, 0) * ((100 - blkCard.SplashResistance) / 100f);
                var actDamage = dmgAmount * (byte.MaxValue / blkCard.Health.MaxHealth);

                checkOpenFaces = (blkCard.IsVisualSlope && blk.VData != 0) || blkCard.IsVisualPrefab ||
                                 blkCard.Visual?.CanBePassedByShot is true;
                if (blk.Damage + actDamage >= byte.MaxValue)
                {
                    dmgTaken = (byte.MaxValue - blk.Damage) * (blkCard.Health.MaxHealth / byte.MaxValue);
                    
                    blk.Id = 0;
                    blk.Damage = 0;
                    blk.VData = 0;
                    blk.Team = TeamType.Neutral;
                
                    OnBlockRemoved(propInfo.prop.Position);
                    StableData(propInfo.prop.Position).Int = uint.MaxValue;
                    dict[propInfo.prop.Position] = blk.ToUpdate(SplashDest);
                    var (cutBlocks, cutRes) = PropagateInstability(propInfo.prop.Position);
                    foreach (var cut in cutBlocks)
                    {
                        dict[cut.Key] = cut.Value;
                    }

                    if (cutRes > 0 && attacker is { OwnerPlayerId: not null })
                    {
                        _mapUpdater.OnCut(attacker.OwnerPlayerId.Value, cutRes);
                    }
                }
                else
                {
                    blk.Damage += (byte) float.Truncate(actDamage);
                    dict[propInfo.prop.Position] = blk.ToUpdate();

                    onlyOpenFaces = checkOpenFaces;
                    if (!onlyOpenFaces)
                        continue;
                }
            }

            var newReduction = dmgReduction + naturalFalloff + 
                               (blkCard.SplashFalloff > 0
                                   ? blkCard.SplashFalloff / 100f
                                   : 0) +
                               (dmgTaken > 0 ? dmgTaken / damage.BlockDamage : 0);
            
            if (newReduction >= 1f || propInfo.travCount == maxTravCount) continue;
            
            foreach (var (dir, index) in propInfo.prop.CanGoDir.Select((i, i1) => (i, i1)))
            {
                if (!dir) continue;
                var direction = CoordsHelper.FaceToVector[index];
                var newPos = direction + propInfo.prop.Position;
                if (visitedBlocks.Contains(newPos) || !ContainsBlock(newPos))
                {
                    continue;
                }

                if (onlyOpenFaces && blk.VData is var vData && blkCard switch
                    {
                        { Visual.CanBePassedByShot: true } => false,
                        { IsVisualSlope: true } => SlopeBuilder.SidesCorners[index].All(c =>
                            SlopeBuilder.IsCorner(c, (byte)vData)),
                        { IsVisualPrefab: true } => PrefabBuilder.IsSolidFace(blk, (BlockFace)index),
                        _ => true
                    })
                {
                    continue;
                }
                
                var newBlock = this[newPos];
                var newBlockCard = newBlock.Card;

                var closestPoint = Vector3.Clamp(locations[0], newPos.ToVector3(), (newPos + Vector3s.One).ToVector3());
                if (Vector3.DistanceSquared(closestPoint, locations[0]) > radiusSqrd ||
                    (!newBlockCard.Destructible && !damage.IgnoreInvincibility))
                {
                    visitedBlocks.Add(newPos);
                    continue;
                }
                
                var newDirCount = propInfo.prop.CanGoDir
                    .Select((c, idx) => c && idx != (int)CoordsHelper.OppositeFace[index]).ToArray();
                
                blockQueue.Enqueue((new SplashDamagePropagation(newPos, newDirCount), propInfo.travCount + 1),
                    onlyOpenFaces || blkCard.Visual?.CanBePassedByShot is true || (checkOpenFaces &&
                        oldVdata is var vdata && !(blkCard switch
                    {
                        { IsVisualSlope: true } => SlopeBuilder.SidesCorners[index].All(c =>
                            SlopeBuilder.IsCorner(c, (byte)vdata)),
                        { IsVisualPrefab: true } => PrefabBuilder.IsSolidFace(blk, (BlockFace)index),
                        _ => true
                    })) ? dmgReduction + naturalFalloff : newReduction);
            }
        }
        
        return (dict, hitUnits);
    }

    public Dictionary<Vector3s, BlockUpdate> HealBlock(Vector3s location, float amount, out float heals)
    {
        var block = this[location];
        var blockCard = block.Card;
        heals = 0;
        var dict = new Dictionary<Vector3s, BlockUpdate>();
        if (block.Damage == 0 || block.IsAir || block.IsLocked || !blockCard.Destructible ||
            !(blockCard.Health?.MaxHealth > 0)) return dict;
        
        var healAmount = amount * (byte.MaxValue / blockCard.Health.MaxHealth);
        if (float.Truncate(healAmount) > block.Damage)
        {
            heals = block.Damage;
            block.Damage = 0;
        }
        else
        {
            block.Damage -= (byte)float.Truncate(healAmount);
            heals = healAmount;
        }
        
        dict[location] = block.ToUpdate();
        return dict;
    }

    public Dictionary<Vector3s, BlockUpdate> RemoveBlock(Vector3s location)
    {
        var block = this[location];
        var dict = new Dictionary<Vector3s, BlockUpdate>();
        if (block.IsAir || block.IsLocked) return dict;
        block.Id = 0;
        block.Damage = 0;
        block.VData = 0;
        block.Team = TeamType.Neutral;
        OnBlockRemoved(location);
        StableData(location).Int = uint.MaxValue;
        dict[location] = block.ToUpdate();
        var (updates, _) = PropagateInstability(location);
        foreach (var blk in updates)
        {
            dict[blk.Key] = blk.Value;
        }
        
        return dict;
    }
    
    private static IEnumerable<Vector3s> StepThroughLine(Vector3 p1, Vector3 p2, float stepSize)
    {
        var direction = p2 - p1;
        var distance = direction.Length();
        var numSteps = (int)Math.Floor(distance / stepSize);
        
        if (distance > 0)
        {
            direction.X /= distance;
            direction.Y /= distance;
            direction.Z /= distance;
        }
        
        for (var i = 0; i <= numSteps; i++)
        {
            var currentDistance = i * stepSize;
            
            yield return new Vector3s(
                p1.X + direction.X * currentDistance,
                p1.Y + direction.Y * currentDistance,
                p1.Z + direction.Z * currentDistance
            );
        }

        // Ensure the exact end point is included if it wasn't perfectly hit by the steps
        if (numSteps * stepSize < distance)
        {
            yield return (Vector3s)p2;
        }
    }

    private bool RaycastCheck(Vector3 start, Vector3 end, float stepAmount, Func<BlockBinary, bool> blockCheck) =>
        StepThroughLine(start, end, stepAmount).All(pos => !ContainsBlock(pos) || blockCheck(this[pos]));

    public HashSet<Unit> CheckVisibility(Vector3 location, IEnumerable<Unit> units, ICollection<Unit> blockingUnits)
    {
        var contained = GetContainedInUnits(blockingUnits, 0, true);
        var check = (BlockBinary block) =>
        {
            var blockCheck = block.IsSolid || block.Card.Visual?.CanBePassedByShot is true;
            var unitCheck = contained.Contains(block.Position);
            return !blockCheck && !unitCheck;
        };
        
        return units.Aggregate(new HashSet<Unit>(), (visibleUnits, unit) =>
        {
            if (RaycastCheck(location, unit.Transform.Position, 1, check)) 
                visibleUnits.Add(unit);
            
            return visibleUnits;
        });
    }

    public bool AttachToBlock(Unit unit, Vector3s location, BlockFace face)
    {
        if (!ContainsBlock(location)) return false;
        
        if (AttachedUnits.TryGetValue(location, out var attachedUnits))
        {
            if (attachedUnits[(int)face] is not null) return false;
            attachedUnits[(int)face] = unit;
            unit.AttachedTo = location;
            return true;
        }
        
        AttachedUnits.Add(location, new Unit?[6]);
        AttachedUnits[location][(int)face] = unit;
        unit.AttachedTo = location;
        return true;
    }

    public void DetachFromBlock(Unit unit, Vector3s location)
    {
        if (!AttachedUnits.TryGetValue(location, out var attachedUnits)) return;
        for (var index = 0; index < attachedUnits.Length; index++)
        {
            var u = attachedUnits[index];
            if (u is not null && u.Id == unit.Id)
            {
                attachedUnits[index] = null;
            }
        }
    }

    public Dictionary<Vector3s, BlockUpdate> MakeSlopeSolid(Vector3s location, Vector3s attachTo)
    {
        var dict = new Dictionary<Vector3s, BlockUpdate>();
        var attachedBlock = this[attachTo];
        if (!ContainsBlock(attachTo) || !attachedBlock.Card.IsVisualSlope) return dict;
        var attachedFace = CoordsHelper.VectorToFace(location - attachTo);
        foreach (var corner in SlopeBuilder.SidesCorners[(int)attachedFace])
        {
            if (SlopeBuilder.IsCorner(corner, (byte)attachedBlock.VData)) continue;
            attachedBlock.VData = 0;
            dict[attachTo] = attachedBlock.ToUpdate();
            UpdateStability(attachTo);
            break;
        }
        return dict;
    }

    private Func<BlockBinary, bool> CanPlaceBlock(CardBlock blockCard, Unit[] unitsInArea, Vector3s? attachTo = null,
        Unit[]? unitsInAttachArea = null)
    {
        Func<BlockBinary, bool> stable = attachTo is null
            ? block => GetBorderingFaces(block.Position, CheckIfStable(block.Position)).Any()
            : block => CheckIfStable(block.Position, ignoreSlope: true)(attachTo.Value);

        Func<BlockBinary, bool> replaceable = block => block.IsReplaceable;
        if (blockCard.Replaceable)
        {
            unitsInArea = unitsInArea.Where(u => u.PlayerId is null).ToArray();
            replaceable = block => block.Card.Replaceable;
        }
        
        var blockedPositions = GetContainedInUnits(unitsInArea, 2, blockCard.Solid, true);
        var blockedAttachPositions =  GetContainedInUnits(unitsInAttachArea ?? [], 2, blockCard.Solid, true);
        return blockCard switch
        {
            { Grounded: true, Solid: true } => block =>
                replaceable(block) && block.Y > 0 &&
                GetValidFacesActual(block.Position with { y = (short)(block.Y - 1) }, true).Contains(BlockFace.Top) &&
                                                             !blockedPositions.Contains(block.Position) &&
                (attachTo is null || !this[attachTo.Value].Card.IsVisualSlope || !blockedAttachPositions.Contains(attachTo.Value)),
            
            { Grounded: true } => block =>
                replaceable(block) && block.Y > 0 && !blockedPositions.Contains(block.Position) &&
                GetValidFacesActual(block.Position with { y = (short)(block.Y - 1) }, true).Contains(BlockFace.Top),
            
            { Solid: true, CanFloat: true } => block =>
                replaceable(block) && !blockedPositions.Contains(block.Position) &&
                (attachTo is null || !this[attachTo.Value].Card.IsVisualSlope || !blockedAttachPositions.Contains(attachTo.Value)),
            
            { Solid: true } => block =>
                replaceable(block) && !blockedPositions.Contains(block.Position) &&
                (attachTo is null || !this[attachTo.Value].Card.IsVisualSlope ||
                 !blockedAttachPositions.Contains(attachTo.Value)) && stable(block),

            { CanFloat: true } => block => replaceable(block) && !blockedPositions.Contains(block.Position),
            
            _ => block => replaceable(block) && !blockedPositions.Contains(block.Position) && stable(block)
        };
    }

    private static Func<BlockBinary, bool> CanReplaceBlock() => block => block.Card is
        {
            Solid: true, Grounded: false, Transparent: false, HasTeam: false, CanFloat: false, Destructible: true,
            IsVisualClone: false
        } or { Visual.Icon: "block_ice" };

    public Vector3s? GetGroundBlockFromSky(int xVal, int zVal)
    {
        for (var y = SizeY - 1; y >= 0; y--)
        {
            var pos = new Vector3s(xVal, y, zVal);
            if (!ContainsBlock(pos)) continue;
            
            var block = this[pos];
            if (block.IsSolid && !block.IsGrounded)
            {
                return block.Position;
            }
        }
        
        return null;
    }

    public ImpactData CreateImpactForBlock(Vector3s blockPos, Vector3 targetPos)
    {
        var owner = OwnedBlocks.GetValueOrDefault(blockPos);
        var block = BlockCardsCache.GetCard(this[blockPos].Id);
        return new ImpactData
        {
            InsidePoint = targetPos,
            Normal = Vector3s.Zero,
            CasterUnitId = owner?.Id,
            CasterPlayerId = owner?.OwnerPlayerId,
            SourceKey = block.Key,
            ShotPos = blockPos.ToVector3(),
            Crit = false
        };
    }

    public bool OnFriendlySide(Vector3 position, TeamType team) =>
        team switch
        {
            TeamType.Neutral => false,
            TeamType.Team1 => position.X <= SizeX * 0.5f,
            TeamType.Team2 => position.X >= SizeX * 0.5f,
            _ => false
        };
    
    public bool OnEnemySide(Vector3 position, TeamType team) =>
        team switch
        {
            TeamType.Neutral => true,
            TeamType.Team1 => position.X > SizeX * 0.5f,
            TeamType.Team2 => position.X < SizeX * 0.5f,
            _ => true
        };
}