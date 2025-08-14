using System.Drawing;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapData
{
    public void SetBarrier(BarrierLabel label, float val)
    {
      if (Properties == null) return;
      switch (label)
      {
        case BarrierLabel.Build1Team1:
          Properties.Barrier1Team1 = val;
          break;
        case BarrierLabel.Build1Team2:
          Properties.Barrier1Team2 = val;
          break;
        case BarrierLabel.Build2Team1:
          Properties.Barrier2Team1 = val;
          break;
        case BarrierLabel.Build2Team2:
          Properties.Barrier2Team2 = val;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(label), label, null);
      }
    }

    public float? GetBarrier(BarrierLabel label)
    {
      return label switch
      {
        BarrierLabel.Build1Team1 => Properties?.Barrier1Team1,
        BarrierLabel.Build1Team2 => Properties?.Barrier1Team2,
        BarrierLabel.Build2Team1 => Properties?.Barrier2Team1,
        BarrierLabel.Build2Team2 => Properties?.Barrier2Team2,
        _ => 0.0f
      };
    }

    public int Version { get; set; }

    public int Schema { get; set; } = 6;

    public MatchType Match { get; set; }

    public List<Color> ColorPalette { get; set; } = [];

    public List<MapSpawnPoint> SpawnPoints { get; set; } = [];

    public List<MapUnit> Units { get; set; } = [];

    public List<MapCamera2> Cameras { get; set; } = [];

    public List<MapTrigger2> Triggers { get; set; } = [];

    public MapDataProps? Properties { get; set; }

    public Vector3s Size { get; set; }

    public byte[]? BlocksData { get; set; }

    public byte[]? ColorsData { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true,
        true, Properties != null, true, BlocksData != null, ColorsData != null).Write(writer);
      writer.Write(Version);
      writer.Write(Schema);
      writer.WriteByteEnum(Match);
      writer.WriteList(ColorPalette, writer.Write);
      writer.WriteList(SpawnPoints, MapSpawnPoint.WriteRecord);
      writer.WriteList(Units, MapUnit.WriteRecord);
      writer.WriteList(Cameras, MapCamera2.WriteRecord);
      writer.WriteList(Triggers, MapTrigger2.WriteVariant);
      if (Properties != null)
        MapDataProps.WriteRecord(writer, Properties);
      writer.Write(Size);
      if (BlocksData != null)
        writer.WriteBinary(BlocksData);
      if (ColorsData == null)
        return;
      writer.WriteBinary(ColorsData);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(12);
      bitField.Read(reader);
      if (bitField[0])
        Version = reader.ReadInt32();
      if (bitField[1])
        Schema = reader.ReadInt32();
      if (bitField[2])
        Match = reader.ReadByteEnum<MatchType>();
      if (bitField[3])
        ColorPalette = reader.ReadList<Color, List<Color>>(reader.ReadColor);
      if (bitField[4])
        SpawnPoints = reader.ReadList<MapSpawnPoint, List<MapSpawnPoint>>(MapSpawnPoint.ReadRecord);
      if (bitField[5])
        Units = reader.ReadList<MapUnit, List<MapUnit>>(MapUnit.ReadRecord);
      if (bitField[6])
        Cameras = reader.ReadList<MapCamera2, List<MapCamera2>>(MapCamera2.ReadRecord);
      if (bitField[7])
        Triggers = reader.ReadList<MapTrigger2, List<MapTrigger2>>(MapTrigger2.ReadVariant);
      Properties = bitField[8] ? MapDataProps.ReadRecord(reader) : null;
      if (bitField[9])
        Size = reader.ReadVector3s();
      BlocksData = bitField[10] ? reader.ReadBinary() : null;
      ColorsData = bitField[11] ? reader.ReadBinary() : null;
    }

    public static void WriteRecord(BinaryWriter writer, MapData value) => value.Write(writer);

    public static MapData ReadRecord(BinaryReader reader)
    {
      var mapData = new MapData();
      mapData.Read(reader);
      return mapData;
    }
}