using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class RespawnLogic
{
    public float BaseRespawnTime { get; set; }

    public List<RespawnIncSequenceItem>? IncrementSequence { get; set; }

    public List<RespawnIncSequenceItem>? IncrementRepeatSequence { get; set; }

    public int? IncrementRepeatLimit { get; set; }

    public float SpawnProtectionSeconds { get; set; }

    public bool BreakProtectionOnAction { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, IncrementSequence != null, IncrementRepeatSequence != null, IncrementRepeatLimit.HasValue, true, true).Write(writer);
      writer.Write(BaseRespawnTime);
      if (IncrementSequence != null)
        writer.WriteList(IncrementSequence, RespawnIncSequenceItem.WriteRecord);
      if (IncrementRepeatSequence != null)
        writer.WriteList(IncrementRepeatSequence, RespawnIncSequenceItem.WriteRecord);
      if (IncrementRepeatLimit.HasValue)
        writer.Write(IncrementRepeatLimit.Value);
      writer.Write(SpawnProtectionSeconds);
      writer.Write(BreakProtectionOnAction);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      if (bitField[0])
        BaseRespawnTime = reader.ReadSingle();
      IncrementSequence = bitField[1] ? reader.ReadList<RespawnIncSequenceItem, List<RespawnIncSequenceItem>>(RespawnIncSequenceItem.ReadRecord) : null;
      IncrementRepeatSequence = bitField[2] ? reader.ReadList<RespawnIncSequenceItem, List<RespawnIncSequenceItem>>(RespawnIncSequenceItem.ReadRecord) : null;
      IncrementRepeatLimit = bitField[3] ? reader.ReadInt32() : null;
      if (bitField[4])
        SpawnProtectionSeconds = reader.ReadSingle();
      if (!bitField[5])
        return;
      BreakProtectionOnAction = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, RespawnLogic value) => value.Write(writer);

    public static RespawnLogic ReadRecord(BinaryReader reader)
    {
      var respawnLogic = new RespawnLogic();
      respawnLogic.Read(reader);
      return respawnLogic;
    }
}