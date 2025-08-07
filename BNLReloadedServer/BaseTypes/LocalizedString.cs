using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LocalizedString
{
    public string? Text { get; set; }

    public Dictionary<Locale, LocalizedEntry>? Data { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true).Write(writer);
      writer.Write(Text!);
      writer.WriteMap(Data!, writer.WriteByteEnum, LocalizedEntry.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      Text = !bitField[0] ? null : reader.ReadString();
      Data = bitField[1] ? reader.ReadMap<Locale, LocalizedEntry, Dictionary<Locale, LocalizedEntry>>(reader.ReadByteEnum<Locale>, LocalizedEntry.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, LocalizedString value)
    {
      value.Write(writer);
    }

    public static LocalizedString ReadRecord(BinaryReader reader)
    {
      var localizedString = new LocalizedString();
      localizedString.Read(reader);
      return localizedString;
    }
}