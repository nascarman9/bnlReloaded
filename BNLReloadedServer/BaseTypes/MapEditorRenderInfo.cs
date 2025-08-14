using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapEditorRenderInfo
{
    public LocalizedString? Name { get; set; }

    public string? Render { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Name != null, Render != null).Write(writer);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Render != null)
        writer.Write(Render);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      Name = !bitField[0] ? null : LocalizedString.ReadRecord(reader);
      Render = bitField[1] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, MapEditorRenderInfo value)
    {
      value.Write(writer);
    }

    public static MapEditorRenderInfo ReadRecord(BinaryReader reader)
    {
      var editorRenderInfo = new MapEditorRenderInfo();
      editorRenderInfo.Read(reader);
      return editorRenderInfo;
    }
}