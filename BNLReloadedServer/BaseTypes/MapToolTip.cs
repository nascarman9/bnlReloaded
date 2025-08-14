using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapToolTip
{
    public string? Image { get; set; }

    public LocalizedString? TipText { get; set; }

    public LocalizedString? TipHeader { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Image != null, TipText != null, TipHeader != null).Write(writer);
      if (Image != null)
        writer.Write(Image);
      if (TipText != null)
        LocalizedString.WriteRecord(writer, TipText);
      if (TipHeader != null)
        LocalizedString.WriteRecord(writer, TipHeader);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Image = bitField[0] ? reader.ReadString() : null;
      TipText = bitField[1] ? LocalizedString.ReadRecord(reader) : null;
      TipHeader = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MapToolTip value) => value.Write(writer);

    public static MapToolTip ReadRecord(BinaryReader reader)
    {
      var mapToolTip = new MapToolTip();
      mapToolTip.Read(reader);
      return mapToolTip;
    }
}