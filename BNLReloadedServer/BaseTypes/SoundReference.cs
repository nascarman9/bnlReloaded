using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SoundReference
{
    public string? ContainerName { get; set; }

    public string? EventName { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(ContainerName != null, EventName != null).Write(writer);
      if (ContainerName != null)
        writer.Write(ContainerName);
      if (EventName != null)
        writer.Write(EventName);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      ContainerName = bitField[0] ? reader.ReadString() : null;
      EventName = bitField[1] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, SoundReference value)
    {
      value.Write(writer);
    }

    public static SoundReference ReadRecord(BinaryReader reader)
    {
      var soundReference = new SoundReference();
      soundReference.Read(reader);
      return soundReference;
    }
}