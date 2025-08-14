using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapEditorDefaultSize
{
    public LocalizedString? Name { get; set; }

    public Vector3s Size { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Name != null, true).Write(writer);
        if (Name != null)
            LocalizedString.WriteRecord(writer, Name);
        writer.Write(Size);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Name = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
        if (!bitField[1])
            return;
        Size = reader.ReadVector3s();
    }

    public static void WriteRecord(BinaryWriter writer, MapEditorDefaultSize value)
    {
        value.Write(writer);
    }

    public static MapEditorDefaultSize ReadRecord(BinaryReader reader)
    {
        var editorDefaultSize = new MapEditorDefaultSize();
        editorDefaultSize.Read(reader);
        return editorDefaultSize;
    }
}