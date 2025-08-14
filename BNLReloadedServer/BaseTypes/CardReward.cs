using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardReward : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Reward;

    public string? ConditionIcon { get; set; }

    public LocalizedString? ConditionText { get; set; }

    public LocalizedString? Message { get; set; }

    public List<Key>? Items { get; set; }

    public Dictionary<CurrencyType, float>? Money { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, ConditionIcon != null, ConditionText != null, Message != null, Items != null, Money != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (ConditionIcon != null)
        writer.Write(ConditionIcon);
      if (ConditionText != null)
        LocalizedString.WriteRecord(writer, ConditionText);
      if (Message != null)
        LocalizedString.WriteRecord(writer, Message);
      if (Items != null)
        writer.WriteList(Items, Key.WriteRecord);
      if (Money != null)
        writer.WriteMap(Money, writer.WriteByteEnum, writer.Write);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      ConditionIcon = bitField[2] ? reader.ReadString() : null;
      ConditionText = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      Message = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
      Items = bitField[5] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Money = bitField[6] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardReward value) => value.Write(writer);

    public static CardReward ReadRecord(BinaryReader reader)
    {
      var cardReward = new CardReward();
      cardReward.Read(reader);
      return cardReward;
    }
}