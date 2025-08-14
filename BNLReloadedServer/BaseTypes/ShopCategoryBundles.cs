using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ShopCategoryBundles : ShopCategory
{
    public override ShopCategoryType Type => ShopCategoryType.Bundles;

    public ShopItemPromotion? Promotion { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Name != null, Items != null, true, Promotion != null).Write(writer);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Items != null)
        writer.WriteList(Items, Key.WriteRecord);
      writer.WriteByteEnum(Label);
      if (Promotion == null)
        return;
      ShopItemPromotion.WriteRecord(writer, Promotion);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Name = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      Items = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[2])
        Label = reader.ReadByteEnum<ShopCategoryLabel>();
      Promotion = bitField[3] ? ShopItemPromotion.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ShopCategoryBundles value)
    {
      value.Write(writer);
    }

    public static ShopCategoryBundles ReadRecord(BinaryReader reader)
    {
      var shopCategoryBundles = new ShopCategoryBundles();
      shopCategoryBundles.Read(reader);
      return shopCategoryBundles;
    }
}