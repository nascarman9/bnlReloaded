using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ShopCategoryFeatured : ShopCategory
{
    public override ShopCategoryType Type => ShopCategoryType.Featured;

    public override void Write(BinaryWriter writer)
    {
      new BitField(Name != null, Items != null, true).Write(writer);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Items != null)
        writer.WriteList(Items, Key.WriteRecord);
      writer.WriteByteEnum(Label);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Name = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      Items = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[2])
        Label = reader.ReadByteEnum<ShopCategoryLabel>();
    }

    public static void WriteRecord(BinaryWriter writer, ShopCategoryFeatured value)
    {
      value.Write(writer);
    }

    public static ShopCategoryFeatured ReadRecord(BinaryReader reader)
    {
      var categoryFeatured = new ShopCategoryFeatured();
      categoryFeatured.Read(reader);
      return categoryFeatured;
    }
}