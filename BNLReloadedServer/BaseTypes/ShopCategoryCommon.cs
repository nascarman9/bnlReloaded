using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ShopCategoryCommon : ShopCategory
{
    public override ShopCategoryType Type => ShopCategoryType.Common;

    public Key? Featured { get; set; }

    public ShopItemPromotion? Promotion { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Name != null, Items != null, true, Featured.HasValue, Promotion != null).Write(writer);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Items != null)
        writer.WriteList(Items, Key.WriteRecord);
      writer.WriteByteEnum(Label);
      if (Featured.HasValue)
        Key.WriteRecord(writer, Featured.Value);
      if (Promotion == null)
        return;
      ShopItemPromotion.WriteRecord(writer, Promotion);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Name = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      Items = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[2])
        Label = reader.ReadByteEnum<ShopCategoryLabel>();
      Featured = bitField[3] ? Key.ReadRecord(reader) : null;
      Promotion = bitField[4] ? ShopItemPromotion.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ShopCategoryCommon value)
    {
      value.Write(writer);
    }

    public static ShopCategoryCommon ReadRecord(BinaryReader reader)
    {
      var shopCategoryCommon = new ShopCategoryCommon();
      shopCategoryCommon.Read(reader);
      return shopCategoryCommon;
    }
}