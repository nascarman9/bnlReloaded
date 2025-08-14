using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardShopLogic : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.ShopLogic;

    public List<Key>? SteamItems { get; set; }

    public ShopData? Shop { get; set; }

    public List<Key>? SteamDlcItems { get; set; }

    public List<MainFeaturedItem>? MainFeatured { get; set; }

    public LocalizedString? InvitingShopMessage { get; set; }

    public float? InvitingShopThreshold { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, SteamItems != null, Shop != null, SteamDlcItems != null, MainFeatured != null,
        InvitingShopMessage != null, InvitingShopThreshold.HasValue).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (SteamItems != null)
        writer.WriteList(SteamItems, Key.WriteRecord);
      if (Shop != null)
        ShopData.WriteRecord(writer, Shop);
      if (SteamDlcItems != null)
        writer.WriteList(SteamDlcItems, Key.WriteRecord);
      if (MainFeatured != null)
        writer.WriteList(MainFeatured, MainFeaturedItem.WriteRecord);
      if (InvitingShopMessage != null)
        LocalizedString.WriteRecord(writer, InvitingShopMessage);
      if (!InvitingShopThreshold.HasValue)
        return;
      writer.Write(InvitingShopThreshold.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      SteamItems = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Shop = bitField[3] ? ShopData.ReadRecord(reader) : null;
      SteamDlcItems = bitField[4] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      MainFeatured = bitField[5] ? reader.ReadList<MainFeaturedItem, List<MainFeaturedItem>>(MainFeaturedItem.ReadRecord) : null;
      InvitingShopMessage = bitField[6] ? LocalizedString.ReadRecord(reader) : null;
      InvitingShopThreshold = bitField[7] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardShopLogic value) => value.Write(writer);

    public static CardShopLogic ReadRecord(BinaryReader reader)
    {
      var cardShopLogic = new CardShopLogic();
      cardShopLogic.Read(reader);
      return cardShopLogic;
    }
}