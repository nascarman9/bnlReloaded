using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SupplyDropsLogic
{
    public List<SupplySequenceItem>? Sequence { get; set; }

    public List<SupplySequenceItem>? RepeatSequence { get; set; }

    public float WarningSeconds { get; set; }

    public float MinimapWarningSeconds { get; set; }

    public float RandomPosOffset { get; set; }

    public float SpawnHeight { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Sequence != null, RepeatSequence != null, true, true, true, true).Write(writer);
      if (Sequence != null)
        writer.WriteList(Sequence, SupplySequenceItem.WriteRecord);
      if (RepeatSequence != null)
        writer.WriteList(RepeatSequence, SupplySequenceItem.WriteRecord);
      writer.Write(WarningSeconds);
      writer.Write(MinimapWarningSeconds);
      writer.Write(RandomPosOffset);
      writer.Write(SpawnHeight);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      Sequence = bitField[0] ? reader.ReadList<SupplySequenceItem, List<SupplySequenceItem>>(SupplySequenceItem.ReadRecord) : null;
      RepeatSequence = bitField[1] ? reader.ReadList<SupplySequenceItem, List<SupplySequenceItem>>(SupplySequenceItem.ReadRecord) : null;
      if (bitField[2])
        WarningSeconds = reader.ReadSingle();
      if (bitField[3])
        MinimapWarningSeconds = reader.ReadSingle();
      if (bitField[4])
        RandomPosOffset = reader.ReadSingle();
      if (!bitField[5])
        return;
      SpawnHeight = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, SupplyDropsLogic value)
    {
      value.Write(writer);
    }

    public static SupplyDropsLogic ReadRecord(BinaryReader reader)
    {
      var supplyDropsLogic = new SupplyDropsLogic();
      supplyDropsLogic.Read(reader);
      return supplyDropsLogic;
    }
}