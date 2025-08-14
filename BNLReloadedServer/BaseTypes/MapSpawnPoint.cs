using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapSpawnPoint
{
    public TeamType Team { get; set; }

    public Vector3 Position { get; set; }

    public Direction2D Direction { get; set; }

    public SpawnPointLabel Label { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true).Write(writer);
      writer.WriteByteEnum(Team);
      writer.Write(Position);
      writer.WriteByteEnum(Direction);
      writer.WriteByteEnum(Label);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        Team = reader.ReadByteEnum<TeamType>();
      if (bitField[1])
        Position = reader.ReadVector3();
      if (bitField[2])
        Direction = reader.ReadByteEnum<Direction2D>();
      if (!bitField[3])
        return;
      Label = reader.ReadByteEnum<SpawnPointLabel>();
    }

    public static void WriteRecord(BinaryWriter writer, MapSpawnPoint value) => value.Write(writer);

    public static MapSpawnPoint ReadRecord(BinaryReader reader)
    {
      var mapSpawnPoint = new MapSpawnPoint();
      mapSpawnPoint.Read(reader);
      return mapSpawnPoint;
    }
}