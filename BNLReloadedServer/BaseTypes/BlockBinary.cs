namespace BNLReloadedServer.BaseTypes;

public class BlockBinary(byte[] data, int index)
{
    public const int Size = 6;
    public readonly ushort Id = BitConverter.ToUInt16(data, index);
    public readonly byte Damage = data[index + 2];
    public readonly ushort VData = BitConverter.ToUInt16(data, index + 3);
    public readonly byte LData = data[index + 5];

    public Block ToBlock()
    {
        return new Block
        {
            Id = Id,
            Damage = Damage,
            Vdata = VData,
            Ldata = LData
        };
    }
}