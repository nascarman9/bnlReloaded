namespace BNLReloadedServer.ProtocolHelpers;

public class BitField
{
    private byte[] _bytes;
    private readonly int _byteCount;

    public BitField(int count)
    {
        Count = count;
        _byteCount = (count >> 3) + (count % 8 == 0 ? 0 : 1);
        _bytes = new byte[_byteCount]; 
    } 

    public BitField(params bool[] values) : this(values.Length)
    {
        var index = 0;
        foreach (var flag in values)
        {
            this[index] = flag;
            ++index;
        }
    }

    public int Count { get; }

    public bool this[int index]
    {
        get => (_bytes[index >> 3] & Mask(index)) != 0;
        set
        {
            var index1 = index >> 3;
            if (value)
                _bytes[index1] |= (byte) Mask(index);
            else
                _bytes[index1] &= (byte) ~Mask(index);
        }
    }

    private static int Mask(int index) => 128 >> (index & 7);

    public void Write(BinaryWriter writer) => writer.Write(_bytes);

    public void Read(BinaryReader reader) => _bytes = reader.ReadBytes(_byteCount);
}