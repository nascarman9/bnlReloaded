using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapUnit
{
    public Vector3 Position { get; set; }

    public Vector3s Rotation { get; set; }

    public Key UnitKey { get; set; }

    public TeamType Team { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true).Write(writer);
      writer.Write(Position);
      writer.Write(Rotation);
      Key.WriteRecord(writer, UnitKey);
      writer.WriteByteEnum(Team);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        Position = reader.ReadVector3();
      if (bitField[1])
        Rotation = reader.ReadVector3s();
      if (bitField[2])
        UnitKey = Key.ReadRecord(reader);
      if (!bitField[3])
        return;
      Team = reader.ReadByteEnum<TeamType>();
    }

    public static void WriteRecord(BinaryWriter writer, MapUnit value) => value.Write(writer);

    public static MapUnit ReadRecord(BinaryReader reader)
    {
      var mapUnit = new MapUnit();
      mapUnit.Read(reader);
      return mapUnit;
    }
}