using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ShopData
{
    public List<ShopCategory>? Categories { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Categories != null).Write(writer);
        if (Categories != null)
            writer.WriteList(Categories, ShopCategory.WriteVariant);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        Categories = bitField[0] ? reader.ReadList<ShopCategory, List<ShopCategory>>(ShopCategory.ReadVariant) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ShopData value) => value.Write(writer);

    public static ShopData ReadRecord(BinaryReader reader)
    {
        var shopData = new ShopData();
        shopData.Read(reader);
        return shopData;
    }
}