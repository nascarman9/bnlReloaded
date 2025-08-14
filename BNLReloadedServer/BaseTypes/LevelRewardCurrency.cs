using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LevelRewardCurrency : LevelReward
{
    public override LevelRewardType Type => LevelRewardType.Currency;

    public Dictionary<CurrencyType, float>? Currency { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Currency != null).Write(writer);
      if (Currency != null)
        writer.WriteMap(Currency, writer.WriteByteEnum, writer.Write);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(1);
      bitField.Read(reader);
      Currency = bitField[0] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, LevelRewardCurrency value)
    {
      value.Write(writer);
    }

    public static LevelRewardCurrency ReadRecord(BinaryReader reader)
    {
      var levelRewardCurrency = new LevelRewardCurrency();
      levelRewardCurrency.Read(reader);
      return levelRewardCurrency;
    }
}