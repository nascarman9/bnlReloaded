using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitLoot
{
    public bool SpawnOnEnemyKill { get; set; }

    public bool SpawnOnFriendlyKill { get; set; }

    public bool SpawnOnUndefinedKill { get; set; }

    public LootItem? LootItem { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, LootItem != null).Write(writer);
      writer.Write(SpawnOnEnemyKill);
      writer.Write(SpawnOnFriendlyKill);
      writer.Write(SpawnOnUndefinedKill);
      if (LootItem != null)
        LootItem.WriteVariant(writer, LootItem);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        SpawnOnEnemyKill = reader.ReadBoolean();
      if (bitField[1])
        SpawnOnFriendlyKill = reader.ReadBoolean();
      if (bitField[2])
        SpawnOnUndefinedKill = reader.ReadBoolean();
      LootItem = bitField[3] ? LootItem.ReadVariant(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitLoot value) => value.Write(writer);

    public static UnitLoot ReadRecord(BinaryReader reader)
    {
      var unitLoot = new UnitLoot();
      unitLoot.Read(reader);
      return unitLoot;
    }
}