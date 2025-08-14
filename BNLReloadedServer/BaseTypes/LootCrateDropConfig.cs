using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootCrateDropConfig
{
    public List<MatchCounter>? Counters { get; set; }

    public float TotalValueRequired { get; set; }

    public Key CrateKey { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Counters != null, true, true).Write(writer);
      if (Counters != null)
        writer.WriteList(Counters, MatchCounter.WriteVariant);
      writer.Write(TotalValueRequired);
      Key.WriteRecord(writer, CrateKey);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Counters = bitField[0] ? reader.ReadList<MatchCounter, List<MatchCounter>>(MatchCounter.ReadVariant) : null;
      if (bitField[1])
        TotalValueRequired = reader.ReadSingle();
      if (!bitField[2])
        return;
      CrateKey = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, LootCrateDropConfig value)
    {
      value.Write(writer);
    }

    public static LootCrateDropConfig ReadRecord(BinaryReader reader)
    {
      var lootCrateDropConfig = new LootCrateDropConfig();
      lootCrateDropConfig.Read(reader);
      return lootCrateDropConfig;
    }
}