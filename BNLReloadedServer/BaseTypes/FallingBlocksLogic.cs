using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class FallingBlocksLogic
{
    public bool ApplyRewardToWholeTeam { get; set; }

    public float ResourceCoeff { get; set; }

    public float ResourceCap { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true).Write(writer);
      writer.Write(ApplyRewardToWholeTeam);
      writer.Write(ResourceCoeff);
      writer.Write(ResourceCap);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        ApplyRewardToWholeTeam = reader.ReadBoolean();
      if (bitField[1])
        ResourceCoeff = reader.ReadSingle();
      if (!bitField[2])
        return;
      ResourceCap = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, FallingBlocksLogic value)
    {
      value.Write(writer);
    }

    public static FallingBlocksLogic ReadRecord(BinaryReader reader)
    {
      var fallingBlocksLogic = new FallingBlocksLogic();
      fallingBlocksLogic.Read(reader);
      return fallingBlocksLogic;
    }
}