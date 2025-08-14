using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardSteamDlcItem : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.SteamDlcItem;

    public uint DlcId { get; set; }

    public LocalizedString? Message { get; set; }

    public Dictionary<CurrencyType, float>? Money { get; set; }

    public List<Key>? Items { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, true, Message != null, Money != null, Items != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      writer.Write(DlcId);
      if (Message != null)
        LocalizedString.WriteRecord(writer, Message);
      if (Money != null)
        writer.WriteMap(Money, writer.WriteByteEnum, writer.Write);
      if (Items != null)
        writer.WriteList(Items, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      if (bitField[2])
        DlcId = reader.ReadUInt32();
      Message = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      Money = bitField[4] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
      Items = bitField[5] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardSteamDlcItem value)
    {
      value.Write(writer);
    }

    public static CardSteamDlcItem ReadRecord(BinaryReader reader)
    {
      var cardSteamDlcItem = new CardSteamDlcItem();
      cardSteamDlcItem.Read(reader);
      return cardSteamDlcItem;
    }
}