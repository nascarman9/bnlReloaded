using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapTriggerSphere : MapTrigger2
{
    public override MapTriggerType2 Type => MapTriggerType2.Sphere;

    public Vector3 Position { get; set; }

    public float Radius { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Tag != null, true, true, true).Write(writer);
      if (Tag != null)
        writer.Write(Tag);
      writer.WriteList(Labels, writer.WriteByteEnum);
      writer.Write(Position);
      writer.Write(Radius);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Tag = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Labels = reader.ReadList<MapTriggerLabel, List<MapTriggerLabel>>(reader.ReadByteEnum<MapTriggerLabel>);
      if (bitField[2])
        Position = reader.ReadVector3();
      if (!bitField[3])
        return;
      Radius = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, MapTriggerSphere value)
    {
      value.Write(writer);
    }

    public static MapTriggerSphere ReadRecord(BinaryReader reader)
    {
      var mapTriggerSphere = new MapTriggerSphere();
      mapTriggerSphere.Read(reader);
      return mapTriggerSphere;
    }
}