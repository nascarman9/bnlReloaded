using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardShopItem : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.ShopItem;

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public LocalizedString? ShortDescription { get; set; }

    public string? Image { get; set; }

    public string? FeaturedImage { get; set; }

    public bool AvailableInShop { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public bool IsNew { get; set; }

    public bool FreeToTry { get; set; }

    public List<Key>? Items { get; set; }

    public Key? Dependency { get; set; }

    public float? DurationHours { get; set; }

    public float? PriceReal { get; set; }

    public float? PriceVirtual { get; set; }

    public ShopItemPromotion? Promotion { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Name != null, Description != null, ShortDescription != null, Image != null,
        FeaturedImage != null, true, ReleaseDate.HasValue, true, true, Items != null, Dependency.HasValue,
        DurationHours.HasValue, PriceReal.HasValue, PriceVirtual.HasValue, Promotion != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (ShortDescription != null)
        LocalizedString.WriteRecord(writer, ShortDescription);
      if (Image != null)
        writer.Write(Image);
      if (FeaturedImage != null)
        writer.Write(FeaturedImage);
      writer.Write(AvailableInShop);
      if (ReleaseDate.HasValue)
        writer.WriteDateTime(ReleaseDate.Value);
      writer.Write(IsNew);
      writer.Write(FreeToTry);
      if (Items != null)
        writer.WriteList(Items, Key.WriteRecord);
      if (Dependency.HasValue)
        Key.WriteRecord(writer, Dependency.Value);
      if (DurationHours.HasValue)
        writer.Write(DurationHours.Value);
      if (PriceReal.HasValue)
        writer.Write(PriceReal.Value);
      if (PriceVirtual.HasValue)
        writer.Write(PriceVirtual.Value);
      if (Promotion == null)
        return;
      ShopItemPromotion.WriteRecord(writer, Promotion);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(17);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Name = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      ShortDescription = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
      Image = bitField[5] ? reader.ReadString() : null;
      FeaturedImage = bitField[6] ? reader.ReadString() : null;
      if (bitField[7])
        AvailableInShop = reader.ReadBoolean();
      ReleaseDate = bitField[8] ? reader.ReadDateTime() : null;
      if (bitField[9])
        IsNew = reader.ReadBoolean();
      if (bitField[10])
        FreeToTry = reader.ReadBoolean();
      Items = bitField[11] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Dependency = bitField[12] ? Key.ReadRecord(reader) : null;
      DurationHours = bitField[13] ? reader.ReadSingle() : null;
      PriceReal = bitField[14] ? reader.ReadSingle() : null;
      PriceVirtual = bitField[15] ? reader.ReadSingle() : null;
      Promotion = bitField[16] ? ShopItemPromotion.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardShopItem value) => value.Write(writer);

    public static CardShopItem ReadRecord(BinaryReader reader)
    {
      var cardShopItem = new CardShopItem();
      cardShopItem.Read(reader);
      return cardShopItem;
    }
}