using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardSteamShopItem : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.SteamShopItem;

    public uint ItemId { get; set; }

    public int Quantity { get; set; } = 1;

    public Dictionary<string, uint>? Cost { get; set; }

    public LocalizedString? Description { get; set; }

    public float RealCurrency { get; set; }

    public string? Icon { get; set; }

    public bool MostPopular { get; set; }

    public bool BestValue { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, true, true, Cost != null, Description != null, true, Icon != null, true, true).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      writer.Write(ItemId);
      writer.Write(Quantity);
      if (Cost != null)
        writer.WriteMap(Cost, writer.Write, writer.Write);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      writer.Write(RealCurrency);
      if (Icon != null)
        writer.Write(Icon);
      writer.Write(MostPopular);
      writer.Write(BestValue);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(10);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      if (bitField[2])
        ItemId = reader.ReadUInt32();
      if (bitField[3])
        Quantity = reader.ReadInt32();
      Cost = bitField[4] ? reader.ReadMap<string, uint, Dictionary<string, uint>>(reader.ReadString, reader.ReadUInt32) : null;
      Description = bitField[5] ? LocalizedString.ReadRecord(reader) : null;
      if (bitField[6])
        RealCurrency = reader.ReadSingle();
      Icon = bitField[7] ? reader.ReadString() : null;
      if (bitField[8])
        MostPopular = reader.ReadBoolean();
      if (!bitField[9])
        return;
      BestValue = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, CardSteamShopItem value)
    {
      value.Write(writer);
    }

    public static CardSteamShopItem ReadRecord(BinaryReader reader)
    {
      var cardSteamShopItem = new CardSteamShopItem();
      cardSteamShopItem.Read(reader);
      return cardSteamShopItem;
    }
}