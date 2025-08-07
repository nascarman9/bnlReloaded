using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SteamLoginInfo
{
    public uint AppId { get; private set; }

    public ulong SteamId { get; private set; }

    public string? Ticket { get; private set; }

    public byte[]? TicketEncrypted { get; private set; }

    public List<uint>? Dlc { get; private set; }

    public string? ContentToken { get; private set; }

    public uint ContentHash { get; private set; }

    public bool ForceRegionSelection { get; private set; }

    private void Write(BinaryWriter writer)
    {
      new BitField(true, true, Ticket != null, TicketEncrypted != null, Dlc != null, ContentToken != null, true, true).Write(writer);
      writer.Write(AppId);
      writer.Write(SteamId);
      if (Ticket != null)
        writer.Write(Ticket);
      if (TicketEncrypted != null)
        writer.WriteArray(TicketEncrypted, writer.Write);
      if (Dlc == null)
        throw new InvalidOperationException("Required property Dlc is null");
      writer.WriteList(Dlc, writer.Write);
      if (ContentToken != null)
        writer.Write(ContentToken);
      writer.Write(ContentHash);
      writer.Write(ForceRegionSelection);
    }

    private void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      if (bitField[0])
        AppId = reader.ReadUInt32();
      if (bitField[1])
        SteamId = reader.ReadUInt64();
      Ticket = bitField[2] ? reader.ReadString() : null;
      TicketEncrypted = bitField[3] ? reader.ReadArray(reader.ReadByte) : null;
      Dlc = bitField[4] ? reader.ReadList<uint, List<uint>>(reader.ReadUInt32) : null;
      ContentToken = bitField[5] ? reader.ReadString() : null;
      if (bitField[6])
        ContentHash = reader.ReadUInt32();
      if (!bitField[7])
        return;
      ForceRegionSelection = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, SteamLoginInfo value)
    {
      value.Write(writer);
    }

    public static SteamLoginInfo ReadRecord(BinaryReader reader)
    {
      var steamLoginInfo = new SteamLoginInfo();
      steamLoginInfo.Read(reader);
      return steamLoginInfo;
    }
}
