using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapEditorModeInfo
{
    public List<Key>? Common { get; set; }

    public List<Key>? Base { get; set; }

    public List<Key>? Line1 { get; set; }

    public List<Key>? Line2 { get; set; }

    public List<Key>? Line3 { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Common != null, Base != null, Line1 != null, Line2 != null, Line3 != null).Write(writer);
      if (Common != null)
        writer.WriteList(Common, Key.WriteRecord);
      if (Base != null)
        writer.WriteList(Base, Key.WriteRecord);
      if (Line1 != null)
        writer.WriteList(Line1, Key.WriteRecord);
      if (Line2 != null)
        writer.WriteList(Line2, Key.WriteRecord);
      if (Line3 != null)
        writer.WriteList(Line3, Key.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Common = bitField[0] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Base = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Line1 = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Line2 = bitField[3] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Line3 = bitField[4] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MapEditorModeInfo value)
    {
      value.Write(writer);
    }

    public static MapEditorModeInfo ReadRecord(BinaryReader reader)
    {
      var mapEditorModeInfo = new MapEditorModeInfo();
      mapEditorModeInfo.Read(reader);
      return mapEditorModeInfo;
    }
}