using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlocksPatternSphere : BlocksPattern
{
  public override BlocksPatternType Type => BlocksPatternType.Sphere;

    public Key BlockKey { get; set; }

    public float Radius { get; set; }

    public float FillRate { get; set; } = 1f;

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true).Write(writer);
      Key.WriteRecord(writer, BlockKey);
      writer.Write(Radius);
      writer.Write(FillRate);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        BlockKey = Key.ReadRecord(reader);
      if (bitField[1])
        Radius = reader.ReadSingle();
      if (!bitField[2])
        return;
      FillRate = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, BlocksPatternSphere value)
    {
      value.Write(writer);
    }

    public static BlocksPatternSphere ReadRecord(BinaryReader reader)
    {
      var blocksPatternSphere = new BlocksPatternSphere();
      blocksPatternSphere.Read(reader);
      return blocksPatternSphere;
    }
}