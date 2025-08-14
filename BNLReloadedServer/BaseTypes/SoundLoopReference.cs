using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SoundLoopReference
{
    public string? ContainerName { get; set; }

    public string? StartEventName { get; set; }

    public string? EndEventName { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(ContainerName != null, StartEventName != null, EndEventName != null).Write(writer);
      if (ContainerName != null)
        writer.Write(ContainerName);
      if (StartEventName != null)
        writer.Write(StartEventName);
      if (EndEventName != null)
        writer.Write(EndEventName);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      ContainerName = bitField[0] ? reader.ReadString() : null;
      StartEventName = bitField[1] ? reader.ReadString() : null;
      EndEventName = bitField[2] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, SoundLoopReference value) => value.Write(writer);

    public static SoundLoopReference ReadRecord(BinaryReader reader)
    {
      var soundLoopReference = new SoundLoopReference();
      soundLoopReference.Read(reader);
      return soundLoopReference;
    }
}