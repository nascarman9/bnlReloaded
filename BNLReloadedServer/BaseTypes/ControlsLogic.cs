using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ControlsLogic
{
    public float HoldDelay { get; set; } = 0.06f;

    public float TogetherDelay { get; set; } = 0.05f;

    public float DoubleClickDelay { get; set; } = 0.2667f;

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true).Write(writer);
      writer.Write(HoldDelay);
      writer.Write(TogetherDelay);
      writer.Write(DoubleClickDelay);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        HoldDelay = reader.ReadSingle();
      if (bitField[1])
        TogetherDelay = reader.ReadSingle();
      if (!bitField[2])
        return;
      DoubleClickDelay = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ControlsLogic value) => value.Write(writer);

    public static ControlsLogic ReadRecord(BinaryReader reader)
    {
      var controlsLogic = new ControlsLogic();
      controlsLogic.Read(reader);
      return controlsLogic;
    }
}