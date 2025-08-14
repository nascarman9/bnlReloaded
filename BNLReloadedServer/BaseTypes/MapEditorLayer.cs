using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapEditorLayer
{
    public Key BlockKey { get; set; }

    public int Count { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        Key.WriteRecord(writer, BlockKey);
        writer.Write(Count);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            BlockKey = Key.ReadRecord(reader);
        if (!bitField[1])
            return;
        Count = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, MapEditorLayer value)
    {
        value.Write(writer);
    }

    public static MapEditorLayer ReadRecord(BinaryReader reader)
    {
        var mapEditorLayer = new MapEditorLayer();
        mapEditorLayer.Read(reader);
        return mapEditorLayer;
    }
}