using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MainFeaturedItem
{
    public LocalizedImage? Image { get; set; }

    public Key? ShopItem { get; set; }

    public string? Url { get; set; }

    public LocalizedString? Header { get; set; }

    public LocalizedString? Text { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Image != null, ShopItem.HasValue, Url != null, Header != null, Text != null).Write(writer);
      if (Image != null)
        LocalizedImage.WriteRecord(writer, Image);
      if (ShopItem.HasValue)
        Key.WriteRecord(writer, ShopItem.Value);
      if (Url != null)
        writer.Write(Url);
      if (Header != null)
        LocalizedString.WriteRecord(writer, Header);
      if (Text != null)
        LocalizedString.WriteRecord(writer, Text);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Image = bitField[0] ? LocalizedImage.ReadRecord(reader) : null;
      ShopItem = bitField[1] ? Key.ReadRecord(reader) : null;
      Url = bitField[2] ? reader.ReadString() : null;
      Header = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      Text = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MainFeaturedItem value)
    {
      value.Write(writer);
    }

    public static MainFeaturedItem ReadRecord(BinaryReader reader)
    {
      var mainFeaturedItem = new MainFeaturedItem();
      mainFeaturedItem.Read(reader);
      return mainFeaturedItem;
    }
}