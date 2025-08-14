using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootEntryRubble : LootEntry
{
    public override LootEntryType Type => LootEntryType.Rubble;

    public BlockCategory? RubbleCategory { get; set; }

    public List<Key>? RubbleKeys { get; set; }

    public int Min { get; set; } = 1;

    public int Max { get; set; } = 1;

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, RubbleCategory.HasValue, RubbleKeys != null, true, true).Write(writer);
      writer.Write(Weight);
      if (RubbleCategory.HasValue)
        writer.WriteByteEnum(RubbleCategory.Value);
      if (RubbleKeys != null)
        writer.WriteList(RubbleKeys, Key.WriteRecord);
      writer.Write(Min);
      writer.Write(Max);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      if (bitField[0])
        Weight = reader.ReadSingle();
      RubbleCategory = bitField[1] ? reader.ReadByteEnum<BlockCategory>() : null;
      RubbleKeys = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[3])
        Min = reader.ReadInt32();
      if (!bitField[4])
        return;
      Max = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, LootEntryRubble value)
    {
      value.Write(writer);
    }

    public static LootEntryRubble ReadRecord(BinaryReader reader)
    {
      var lootEntryRubble = new LootEntryRubble();
      lootEntryRubble.Read(reader);
      return lootEntryRubble;
    }
}