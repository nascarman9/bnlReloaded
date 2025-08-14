using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class HerculesMetadata
{
    public string? User { get; set; }

    public string? Timestamp { get; set; }

    public string? PrevRev { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(User != null, Timestamp != null, PrevRev != null).Write(writer);
      if (User != null)
        writer.Write(User);
      if (Timestamp != null)
        writer.Write(Timestamp);
      if (PrevRev != null)
        writer.Write(PrevRev);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      User = bitField[0] ? reader.ReadString() : null;
      Timestamp = bitField[1] ? reader.ReadString() : null;
      PrevRev = bitField[2] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, HerculesMetadata value)
    {
      value.Write(writer);
    }

    public static HerculesMetadata ReadRecord(BinaryReader reader)
    {
      var herculesMetadata = new HerculesMetadata();
      herculesMetadata.Read(reader);
      return herculesMetadata;
    }
}