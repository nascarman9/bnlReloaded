namespace BNLReloadedServer.BaseTypes;

public class BlockArrayMap3D : BlockMap3D
{
    private Block[][][] storage;

    public BlockArrayMap3D(Vector3s size)
        : this(size.x, size.y, size.z)
    {
    }

    public BlockArrayMap3D(int sizeX, int sizeY, int sizeZ)
        : base(sizeX, sizeY, sizeZ)
    {
        storage = new Block[SizeX][][];
        for (var index1 = 0; index1 < SizeX; ++index1)
        {
            storage[index1] = new Block[SizeY][];
            for (var index2 = 0; index2 < SizeY; ++index2)
                storage[index1][index2] = new Block[SizeZ];
        }
    }

    protected override Block Get(int x, int y, int z) => storage[x][y][z];

    protected override void Set(int x, int y, int z, Block block) => storage[x][y][z] = block;
    
    public override IEnumerator<Block> GetEnumerator()
    {
        for (var x = 0; x < SizeX; ++x)
        {
            for (var y = 0; y < SizeY; ++y)
            {
                for (var z = 0; z < SizeZ; ++z)
                {
                    yield return storage[x][y][z];
                }
            }
        }
    }
}