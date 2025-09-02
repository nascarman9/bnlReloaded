using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapBinary
{
  private byte[] data;

  public MapBinary(byte[] binary)
  {
    var binaryReader = new BinaryReader(binary.UnZip());
    SizeX = binaryReader.ReadUInt16();
    SizeY = binaryReader.ReadUInt16();
    SizeZ = binaryReader.ReadUInt16();
    var count = SizeX * SizeY * SizeZ * 6;
    data = new byte[count];
    if (binaryReader.Read(data, 0, count) != count)
      throw new EndOfStreamException();
  }

  public MapBinary(int schema, byte[] binary, Vector3s size)
  {
    var input = binary.UnZip();
    var binaryReader = new BinaryReader(input);
    SizeX = size.x;
    SizeY = size.y;
    SizeZ = size.z;
    var count = SizeX * SizeY * SizeZ * 6;
    data = new byte[count];
    if (SizeX * SizeY * SizeZ * 4 == input.Length)
    {
      for (var index = 0; index < SizeX * SizeY * SizeZ; ++index)
      {
        var bytes1 = BitConverter.GetBytes((ushort) binaryReader.ReadByte());
        data[index * 6] = bytes1[0];
        data[index * 6 + 1] = bytes1[1];
        data[index * 6 + 2] = binaryReader.ReadByte();
        var bytes2 = BitConverter.GetBytes((ushort) binaryReader.ReadByte());
        data[index * 6 + 3] = bytes2[0];
        data[index * 6 + 4] = bytes2[1];
        data[index * 6 + 5] = binaryReader.ReadByte();
      }
    }
    else if (SizeX * SizeY * SizeZ * 5 == input.Length)
    {
      for (var index = 0; index < SizeX * SizeY * SizeZ; ++index)
      {
        var bytes3 = BitConverter.GetBytes(binaryReader.ReadUInt16());
        data[index * 6] = bytes3[0];
        data[index * 6 + 1] = bytes3[1];
        data[index * 6 + 2] = binaryReader.ReadByte();
        var bytes4 = BitConverter.GetBytes((ushort) binaryReader.ReadByte());
        data[index * 6 + 3] = bytes4[0];
        data[index * 6 + 4] = bytes4[1];
        data[index * 6 + 5] = binaryReader.ReadByte();
      }
    }
    else if (binaryReader.Read(data, 0, count) != count)
      throw new EndOfStreamException();
  }

  public int SizeX { get; }

  public int SizeY { get; }

  public int SizeZ { get; }

  public BlockBinary this[Vector3s pos]
  {
    get => this[pos.x, pos.y, pos.z];
    set => this[pos.x, pos.y, pos.z] = value;
  }

  public BlockBinary this[int x, int y, int z]
  {
    get => new(data, ((x * SizeY + y) * SizeZ + z) * 6);
    set
    {
    }
  }

  public Vector3s Size => new(SizeX, SizeY, SizeZ);

  public BlockArrayMap3D ToMap3D()
  {
    var map3D = new BlockArrayMap3D(Size);
    map3D.Change((ref Block value, ref Vector3s pos) => value = this[pos].ToBlock());
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
    binaryWriter.Write((ushort) SizeX);
    binaryWriter.Write((ushort) SizeY);
    binaryWriter.Write((ushort) SizeZ);
    binaryWriter.Write(data);
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
}