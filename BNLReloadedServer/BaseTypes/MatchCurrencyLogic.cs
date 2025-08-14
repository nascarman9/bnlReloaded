using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchCurrencyLogic
{
    public Dictionary<CurrencyType, float>? CurrencyPerMinute { get; set; }

    public Dictionary<CurrencyType, float>? MinCap { get; set; }

    public Dictionary<CurrencyType, float>? MaxCap { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(CurrencyPerMinute != null, MinCap != null, MaxCap != null).Write(writer);
      if (CurrencyPerMinute != null)
        writer.WriteMap(CurrencyPerMinute, writer.WriteByteEnum, writer.Write);
      if (MinCap != null)
        writer.WriteMap(MinCap, writer.WriteByteEnum, writer.Write);
      if (MaxCap != null)
        writer.WriteMap(MaxCap, writer.WriteByteEnum, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      CurrencyPerMinute = bitField[0] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
      MinCap = bitField[1] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
      MaxCap = bitField[2] ? reader.ReadMap<CurrencyType, float, Dictionary<CurrencyType, float>>(reader.ReadByteEnum<CurrencyType>, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchCurrencyLogic value)
    {
      value.Write(writer);
    }

    public static MatchCurrencyLogic ReadRecord(BinaryReader reader)
    {
      var matchCurrencyLogic = new MatchCurrencyLogic();
      matchCurrencyLogic.Read(reader);
      return matchCurrencyLogic;
    }
}