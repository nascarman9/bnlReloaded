using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZoneInitData
{
    public Key? MapKey { get; set; }

    public MapData? Map { get; set; }

    public byte[]? MapData { get; set; }

    public byte[]? ColorData { get; set; }

    public Dictionary<Vector3s, BlockUpdate>? Updates { get; set; }

    public bool CanSwitchHero { get; set; }

    public bool IsCustomGame { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(MapKey.HasValue, Map != null, MapData != null, ColorData != null, Updates != null, true, true).Write(writer);
      if (MapKey.HasValue)
        Key.WriteRecord(writer, MapKey.Value);
      if (Map != null)
        BaseTypes.MapData.WriteRecord(writer, Map);
      if (MapData != null)
        writer.WriteBinary(MapData);
      if (ColorData != null)
        writer.WriteBinary(ColorData);
      if (Updates != null)
        writer.WriteMap(Updates, writer.Write, BlockUpdate.WriteRecord);
      writer.Write(CanSwitchHero);
      writer.Write(IsCustomGame);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      MapKey = bitField[0] ? Key.ReadRecord(reader) : null;
      Map = bitField[1] ? BaseTypes.MapData.ReadRecord(reader) : null;
      MapData = bitField[2] ? reader.ReadBinary() : null;
      ColorData = bitField[3] ? reader.ReadBinary() : null;
      Updates = bitField[4] ? reader.ReadMap<Vector3s, BlockUpdate, Dictionary<Vector3s, BlockUpdate>>(reader.ReadVector3s, BlockUpdate.ReadRecord) : null;
      if (bitField[5])
        CanSwitchHero = reader.ReadBoolean();
      if (!bitField[6])
        return;
      IsCustomGame = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, ZoneInitData value) => value.Write(writer);

    public static ZoneInitData ReadRecord(BinaryReader reader)
    {
      var zoneInitData = new ZoneInitData();
      zoneInitData.Read(reader);
      return zoneInitData;
    }
}