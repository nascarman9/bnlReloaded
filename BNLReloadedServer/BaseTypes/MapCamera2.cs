using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapCamera2
{
    public Vector3 Direction { get; set; }

    public Vector3 Position { get; set; }

    public TeamType Team { get; set; }

    public List<MapCameraLabel> Labels { get; set; } = [];

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true).Write(writer);
      writer.Write(Direction);
      writer.Write(Position);
      writer.WriteByteEnum(Team);
      writer.WriteList(Labels, writer.WriteByteEnum);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        Direction = reader.ReadVector3();
      if (bitField[1])
        Position = reader.ReadVector3();
      if (bitField[2])
        Team = reader.ReadByteEnum<TeamType>();
      if (bitField[3])
        Labels = reader.ReadList<MapCameraLabel, List<MapCameraLabel>>(reader.ReadByteEnum<MapCameraLabel>);
    }

    public static void WriteRecord(BinaryWriter writer, MapCamera2 value) => value.Write(writer);

    public static MapCamera2 ReadRecord(BinaryReader reader)
    {
      var mapCamera2 = new MapCamera2();
      mapCamera2.Read(reader);
      return mapCamera2;
    }
}