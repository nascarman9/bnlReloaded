using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapCustomData()
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool DefaultImage { get; set; } = true;

    public ulong? PublishId { get; set; }

    public bool IsPublished { get; set; }

    public string? MapId { get; set; }

    public MapData? Map { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, PublishId.HasValue, true, MapId != null, Map != null).Write(writer);
      writer.Write(Name);
      writer.Write(Description);
      writer.Write(DefaultImage);
      if (PublishId.HasValue)
        writer.Write(PublishId.Value);
      writer.Write(IsPublished);
      if (MapId != null)
        writer.Write(MapId);
      if (Map != null)
        MapData.WriteRecord(writer, Map);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Name = reader.ReadString();
      Description = reader.ReadString();
      if (bitField[2])
        DefaultImage = reader.ReadBoolean();
      PublishId = bitField[3] ? reader.ReadUInt64() : null;
      if (bitField[4])
        IsPublished = reader.ReadBoolean();
      MapId = bitField[5] ? reader.ReadString() : null;
      Map = bitField[6] ? MapData.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MapCustomData value) => value.Write(writer);

    public static MapCustomData ReadRecord(BinaryReader reader)
    {
      var mapCustomData = new MapCustomData();
      mapCustomData.Read(reader);
      return mapCustomData;
    }
}