using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LocalizedEntry
{
    public string? Original { get; set; }

    public string? Translation { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true).Write(writer);
      writer.Write(Original!);
      writer.Write(Translation!);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      Original = bitField[0] ? reader.ReadString() : null;
      Translation = bitField[1] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, LocalizedEntry value)
    {
      value.Write(writer);
    }

    public static LocalizedEntry ReadRecord(BinaryReader reader)
    {
      var localizedEntry = new LocalizedEntry();
      localizedEntry.Read(reader);
      return localizedEntry;
    }
}