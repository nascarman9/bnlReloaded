using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapEditorItemList
{
    public LocalizedString? Name { get; set; }

    public List<Key>? Items { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Name != null, Items != null).Write(writer);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Items != null)
        writer.WriteList(Items, Key.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      Name = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      Items = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MapEditorItemList value)
    {
      value.Write(writer);
    }

    public static MapEditorItemList ReadRecord(BinaryReader reader)
    {
      var mapEditorItemList = new MapEditorItemList();
      mapEditorItemList.Read(reader);
      return mapEditorItemList;
    }
}