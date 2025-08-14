using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchResourceTicker
{
    public float ResourcePerSec { get; set; }

    public float? IncreaseInterval { get; set; }

    public float? ResourceIncreaseAmount { get; set; }

    public float? BonusIncreaseAmount { get; set; }

    public int? IncreaseNumberLimit { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, IncreaseInterval.HasValue, ResourceIncreaseAmount.HasValue, BonusIncreaseAmount.HasValue,
        IncreaseNumberLimit.HasValue).Write(writer);
      writer.Write(ResourcePerSec);
      if (IncreaseInterval.HasValue)
        writer.Write(IncreaseInterval.Value);
      if (ResourceIncreaseAmount.HasValue)
        writer.Write(ResourceIncreaseAmount.Value);
      if (BonusIncreaseAmount.HasValue)
        writer.Write(BonusIncreaseAmount.Value);
      if (!IncreaseNumberLimit.HasValue)
        return;
      writer.Write(IncreaseNumberLimit.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      if (bitField[0])
        ResourcePerSec = reader.ReadSingle();
      IncreaseInterval = bitField[1] ? reader.ReadSingle() : null;
      ResourceIncreaseAmount = bitField[2] ? reader.ReadSingle() : null;
      BonusIncreaseAmount = bitField[3] ? reader.ReadSingle() : null;
      IncreaseNumberLimit = bitField[4] ? reader.ReadInt32() : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchResourceTicker value)
    {
      value.Write(writer);
    }

    public static MatchResourceTicker ReadRecord(BinaryReader reader)
    {
      var matchResourceTicker = new MatchResourceTicker();
      matchResourceTicker.Read(reader);
      return matchResourceTicker;
    }
}