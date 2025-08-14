using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ShopItemPromotion
{
    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public float RealDiscount { get; set; }

    public float VirtualDiscount { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(From.HasValue, To.HasValue, true, true).Write(writer);
      if (From.HasValue)
        writer.WriteDateTime(From.Value);
      if (To.HasValue)
        writer.WriteDateTime(To.Value);
      writer.Write(RealDiscount);
      writer.Write(VirtualDiscount);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      From = bitField[0] ? reader.ReadDateTime() : null;
      To = bitField[1] ? reader.ReadDateTime() : null;
      if (bitField[2])
        RealDiscount = reader.ReadSingle();
      if (!bitField[3])
        return;
      VirtualDiscount = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ShopItemPromotion value)
    {
      value.Write(writer);
    }

    public static ShopItemPromotion ReadRecord(BinaryReader reader)
    {
      var shopItemPromotion = new ShopItemPromotion();
      shopItemPromotion.Read(reader);
      return shopItemPromotion;
    }
}