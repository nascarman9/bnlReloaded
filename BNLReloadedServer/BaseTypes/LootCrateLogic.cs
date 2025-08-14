using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootCrateLogic
{
    public int FreeCrateCooldown { get; set; } = 14400;

    public Key FreeCrateKey { get; set; }

    public int ItemsInCrate { get; set; } = 4;

    public List<LootCrateDropConfig> DropConfigs { get; set; } = [];

    public int MinGoldForEmptyLootEntry { get; set; } = 50;

    public int MaxGoldForEmptyLootEntry { get; set; } = 200;

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true).Write(writer);
      writer.Write(FreeCrateCooldown);
      Key.WriteRecord(writer, FreeCrateKey);
      writer.Write(ItemsInCrate);
      writer.WriteList(DropConfigs, LootCrateDropConfig.WriteRecord);
      writer.Write(MinGoldForEmptyLootEntry);
      writer.Write(MaxGoldForEmptyLootEntry);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      if (bitField[0])
        FreeCrateCooldown = reader.ReadInt32();
      if (bitField[1])
        FreeCrateKey = Key.ReadRecord(reader);
      if (bitField[2])
        ItemsInCrate = reader.ReadInt32();
      if (bitField[3])
        DropConfigs = reader.ReadList<LootCrateDropConfig, List<LootCrateDropConfig>>(LootCrateDropConfig.ReadRecord);
      if (bitField[4])
        MinGoldForEmptyLootEntry = reader.ReadInt32();
      if (!bitField[5])
        return;
      MaxGoldForEmptyLootEntry = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, LootCrateLogic value)
    {
      value.Write(writer);
    }

    public static LootCrateLogic ReadRecord(BinaryReader reader)
    {
      var lootCrateLogic = new LootCrateLogic();
      lootCrateLogic.Read(reader);
      return lootCrateLogic;
    }
}