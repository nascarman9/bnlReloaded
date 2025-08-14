using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class DebugServerNode
{
    public string? Id { get; set; }

    public string? Node { get; set; }

    public List<DebugServerNode>? Childs { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Id != null, Node != null, Childs != null).Write(writer);
        if (Id != null)
            writer.Write(Id);
        if (Node != null)
            writer.Write(Node);
        if (Childs != null)
            writer.WriteList(Childs, WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        Id = bitField[0] ? reader.ReadString() : null;
        Node = bitField[1] ? reader.ReadString() : null;
        Childs = bitField[2] ? reader.ReadList<DebugServerNode, List<DebugServerNode>>(ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, DebugServerNode value)
    {
        value.Write(writer);
    }

    public static DebugServerNode ReadRecord(BinaryReader reader)
    {
        var debugServerNode = new DebugServerNode();
        debugServerNode.Read(reader);
        return debugServerNode;
    }
}