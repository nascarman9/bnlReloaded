using System.Collections;

namespace BNLReloadedServer.BaseTypes;

public abstract class BlockMap3D(int sizeX, int sizeY, int sizeZ) : IEnumerable<Block>
{
    public BlockMap3D(Vector3s size)
        : this(size.x, size.y, size.z)
    {
    }

    public int SizeX { get; } = sizeX;

    public int SizeY { get; } = sizeY;

    public int SizeZ { get; } = sizeZ;

    protected abstract Block Get(int x, int y, int z);

    protected abstract void Set(int x, int y, int z, Block block);

    public Block this[Vector3s pos]
    {
        get => Get(pos.x, pos.y, pos.z);
        set => Set(pos.x, pos.y, pos.z, value);
    }

    public Block this[int x, int y, int z]
    {
        get => Get(x, y, z);
        set => Set(x, y, z, value);
    }

    public void Change(DelegateChange update)
    {
        var zero = Vector3s.Zero;
        for (zero.x = 0; zero.x < SizeX; ++zero.x)
        {
            for (zero.y = 0; zero.y < SizeY; ++zero.y)
            {
                for (zero.z = 0; zero.z < SizeZ; ++zero.z)
                {
                    var block = Get(zero.x, zero.y, zero.z);
                    update(ref block, ref zero);
                    Set(zero.x, zero.y, zero.z, block);
                }
            }
        }
    }

    public int Count => SizeX * SizeY * SizeZ;

    public Vector3s Size => new(SizeX, SizeY, SizeZ);

    public bool Check(Vector3s pos)
    {
        return 0 <= pos.x && pos.x < SizeX && 0 <= pos.y && pos.y < SizeY && 0 <= pos.z && pos.z < SizeZ;
    }
    
    public delegate void DelegateChange(ref Block value, ref Vector3s pos);

    public abstract IEnumerator<Block> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}