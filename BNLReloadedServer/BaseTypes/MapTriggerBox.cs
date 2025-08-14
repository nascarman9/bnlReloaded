using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapTriggerBox : MapTrigger2
{
    public override MapTriggerType2 Type => MapTriggerType2.Box;

    public Vector3 Position { get; set; }

    public Vector3 Size { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Tag != null, true, true, true).Write(writer);
      if (Tag != null)
        writer.Write(Tag);
      writer.WriteList(Labels, writer.WriteByteEnum);
      writer.Write(Position);
      writer.Write(Size);
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
      Size = reader.ReadVector3();
    }

    public static void WriteRecord(BinaryWriter writer, MapTriggerBox value) => value.Write(writer);

    public static MapTriggerBox ReadRecord(BinaryReader reader)
    {
      var mapTriggerBox = new MapTriggerBox();
      mapTriggerBox.Read(reader);
      return mapTriggerBox;
    }
}