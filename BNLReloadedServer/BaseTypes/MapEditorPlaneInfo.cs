using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapEditorPlaneInfo
{
    public LocalizedString? Name { get; set; }

    public string? Prefab { get; set; }

    public float DefaultPlaneHeight { get; set; }

    public float DefaultKillHeight { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Name != null, Prefab != null, true, true).Write(writer);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Prefab != null)
        writer.Write(Prefab);
      writer.Write(DefaultPlaneHeight);
      writer.Write(DefaultKillHeight);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Name = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      Prefab = bitField[1] ? reader.ReadString() : null;
      if (bitField[2])
        DefaultPlaneHeight = reader.ReadSingle();
      if (!bitField[3])
        return;
      DefaultKillHeight = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, MapEditorPlaneInfo value)
    {
      value.Write(writer);
    }

    public static MapEditorPlaneInfo ReadRecord(BinaryReader reader)
    {
      var mapEditorPlaneInfo = new MapEditorPlaneInfo();
      mapEditorPlaneInfo.Read(reader);
      return mapEditorPlaneInfo;
    }
}