using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CurrencyLogic
{
    public Dictionary<CurrencyType, CurrencyInfo>? CurrencyInfo { get; set; }

    public Dictionary<CurrencyType, float>? InitialCurrency { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(CurrencyInfo != null, InitialCurrency != null).Write(writer);
      if (CurrencyInfo != null)
        writer.WriteMap(CurrencyInfo, writer.WriteByteEnum, BaseTypes.CurrencyInfo.WriteRecord);
      if (InitialCurrency != null)
        writer.WriteMap(InitialCurrency, writer.WriteByteEnum, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      CurrencyInfo = bitField[0] ? reader.ReadMap<CurrencyType, CurrencyInfo, Dictionary<CurrencyType, CurrencyInfo>>(reader.ReadByteEnum<CurrencyType>, BaseTypes.CurrencyInfo.ReadRecord) : null;
      InitialCurrency = bitField[1] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CurrencyLogic value) => value.Write(writer);

    public static CurrencyLogic ReadRecord(BinaryReader reader)
    {
      var currencyLogic = new CurrencyLogic();
      currencyLogic.Read(reader);
      return currencyLogic;
    }
}