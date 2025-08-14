using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneUpdate
{
    public ZonePhase? Phase { get; set; }

    public MatchStats? Statistics { get; set; }

    public List<SpawnPoint>? SpawnPoints { get; set; }

    public Dictionary<uint, uint?>? PlayerSpawnPoints { get; set; }

    public Dictionary<uint, ulong>? RespawnInfo { get; set; }

    public Dictionary<uint, ZonePlayerInfo>? PlayerInfo { get; set; }

    public SupplyInfo? SupplyInfo { get; set; }

    public List<ZoneObjective>? Objectives { get; set; }

    public float? ResourceCap { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Phase != null, Statistics != null, SpawnPoints != null, PlayerSpawnPoints != null,
        RespawnInfo != null, PlayerInfo != null, SupplyInfo != null, Objectives != null,
        ResourceCap.HasValue).Write(writer);
      if (Phase != null)
        ZonePhase.WriteRecord(writer, Phase);
      if (Statistics != null)
        MatchStats.WriteRecord(writer, Statistics);
      if (SpawnPoints != null)
        writer.WriteList(SpawnPoints, SpawnPoint.WriteRecord);
      if (PlayerSpawnPoints != null)
        writer.WriteMap(PlayerSpawnPoints, writer.Write, item => writer.WriteOptionValue(item, writer.Write));
      if (RespawnInfo != null)
        writer.WriteMap(RespawnInfo, writer.Write, writer.Write);
      if (PlayerInfo != null)
        writer.WriteMap(PlayerInfo, writer.Write, ZonePlayerInfo.WriteRecord);
      if (SupplyInfo != null)
        SupplyInfo.WriteRecord(writer, SupplyInfo);
      if (Objectives != null)
        writer.WriteList(Objectives, ZoneObjective.WriteRecord);
      if (!ResourceCap.HasValue)
        return;
      writer.Write(ResourceCap.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(9);
      bitField.Read(reader);
      Phase = bitField[0] ? ZonePhase.ReadRecord(reader) : null;
      Statistics = bitField[1] ? MatchStats.ReadRecord(reader) : null;
      SpawnPoints = bitField[2] ? reader.ReadList<SpawnPoint, List<SpawnPoint>>(SpawnPoint.ReadRecord) : null;
      PlayerSpawnPoints = bitField[3] ? reader.ReadMap<uint, uint?, Dictionary<uint, uint?>>(reader.ReadUInt32, () => reader.ReadOptionValue(reader.ReadUInt32)) : null;
      RespawnInfo = bitField[4] ? reader.ReadMap<uint, ulong, Dictionary<uint, ulong>>(reader.ReadUInt32, reader.ReadUInt64) : null;
      PlayerInfo = bitField[5] ? reader.ReadMap<uint, ZonePlayerInfo, Dictionary<uint, ZonePlayerInfo>>(reader.ReadUInt32, ZonePlayerInfo.ReadRecord) : null;
      SupplyInfo = bitField[6] ? SupplyInfo.ReadRecord(reader) : null;
      Objectives = bitField[7] ? reader.ReadList<ZoneObjective, List<ZoneObjective>>(ZoneObjective.ReadRecord) : null;
      ResourceCap = bitField[8] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, ZoneUpdate value) => value.Write(writer);

    public static ZoneUpdate ReadRecord(BinaryReader reader)
    {
      var zoneUpdate = new ZoneUpdate();
      zoneUpdate.Read(reader);
      return zoneUpdate;
    }
}