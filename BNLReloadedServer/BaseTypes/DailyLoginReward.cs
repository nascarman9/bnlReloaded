using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class DailyLoginReward
{
    public LocalizedString? Caption { get; set; }

    public string? Icon { get; set; }

    public List<Key>? Items { get; set; }

    public Dictionary<CurrencyType, float>? Currency { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Caption != null, Icon != null, Items != null, Currency != null).Write(writer);
      if (Caption != null)
        LocalizedString.WriteRecord(writer, Caption);
      if (Icon != null)
        writer.Write(Icon);
      if (Items != null)
        writer.WriteList(Items, Key.WriteRecord);
      if (Currency != null)
        writer.WriteMap(Currency, writer.WriteByteEnum, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Caption = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      Icon = bitField[1] ? reader.ReadString() : null;
      Items = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Currency = bitField[3] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, DailyLoginReward value)
    {
      value.Write(writer);
    }

    public static DailyLoginReward ReadRecord(BinaryReader reader)
    {
      var dailyLoginReward = new DailyLoginReward();
      dailyLoginReward.Read(reader);
      return dailyLoginReward;
    }
}