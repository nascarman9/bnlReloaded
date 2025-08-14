using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LocalizedImage
{
    public string? Original { get; set; }

    public Dictionary<Locale, string>? Data { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Original != null, Data != null).Write(writer);
      if (Original != null)
        writer.Write(Original);
      if (Data == null)
        return;
      writer.WriteMap(Data, writer.WriteByteEnum, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      Original = bitField[0] ? reader.ReadString() : null;
      Data = bitField[1] ? reader.ReadMap<Locale, string, Dictionary<Locale, string>>(reader.ReadByteEnum<Locale>, reader.ReadString) : null;
    }

    public static void WriteRecord(BinaryWriter writer, LocalizedImage value)
    {
      value.Write(writer);
    }

    public static LocalizedImage ReadRecord(BinaryReader reader)
    {
      var localizedImage = new LocalizedImage();
      localizedImage.Read(reader);
      return localizedImage;
    }
}