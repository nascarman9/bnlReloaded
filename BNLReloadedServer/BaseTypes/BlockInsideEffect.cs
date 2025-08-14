using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlockInsideEffect
{
    public InstEffect? Effect { get; set; }

    public bool TargetSelf { get; set; }

    public bool TargetUnit { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Effect != null, true, true).Write(writer);
      if (Effect != null)
        InstEffect.WriteVariant(writer, Effect);
      writer.Write(TargetSelf);
      writer.Write(TargetUnit);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Effect = bitField[0] ? InstEffect.ReadVariant(reader) : null;
      if (bitField[1])
        TargetSelf = reader.ReadBoolean();
      if (!bitField[2])
        return;
      TargetUnit = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, BlockInsideEffect value)
    {
      value.Write(writer);
    }

    public static BlockInsideEffect ReadRecord(BinaryReader reader)
    {
      var blockInsideEffect = new BlockInsideEffect();
      blockInsideEffect.Read(reader);
      return blockInsideEffect;
    }
}