using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchmakerConfig
{
    public int QueueMinSlot { get; set; }

    public int QueueMaxSlot { get; set; } = 60;

    public int QueueSlotSize { get; set; } = 50;

    public List<RealmConfig>? Realms { get; set; }

    public Dictionary<int, float>? SquadBoostByMmr { get; set; }

    public Dictionary<int, float>? SquadBoostBySize { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, Realms != null, SquadBoostByMmr != null, SquadBoostBySize != null).Write(writer);
      writer.Write(QueueMinSlot);
      writer.Write(QueueMaxSlot);
      writer.Write(QueueSlotSize);
      if (Realms != null)
        writer.WriteList(Realms, RealmConfig.WriteRecord);
      if (SquadBoostByMmr != null)
        writer.WriteMap(SquadBoostByMmr, writer.Write, writer.Write);
      if (SquadBoostBySize != null)
        writer.WriteMap(SquadBoostBySize, writer.Write, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      if (bitField[0])
        QueueMinSlot = reader.ReadInt32();
      if (bitField[1])
        QueueMaxSlot = reader.ReadInt32();
      if (bitField[2])
        QueueSlotSize = reader.ReadInt32();
      Realms = bitField[3] ? reader.ReadList<RealmConfig, List<RealmConfig>>(RealmConfig.ReadRecord) : null;
      SquadBoostByMmr = bitField[4] ? reader.ReadMap<int, float, Dictionary<int, float>>(reader.ReadInt32, reader.ReadSingle) : null;
      SquadBoostBySize = bitField[5] ? reader.ReadMap<int, float, Dictionary<int, float>>(reader.ReadInt32, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchmakerConfig value)
    {
      value.Write(writer);
    }

    public static MatchmakerConfig ReadRecord(BinaryReader reader)
    {
      var matchmakerConfig = new MatchmakerConfig();
      matchmakerConfig.Read(reader);
      return matchmakerConfig;
    }
}