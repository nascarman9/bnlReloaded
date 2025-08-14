using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BuildInfo
{
    public byte ToolIndex { get; set; }

    public Key DeviceKey { get; set; }

    public Vector3s BuildInsidePosition { get; set; }

    public Vector3s BuildOutsidePosition { get; set; }

    public Direction2D Direction { get; set; }

    public bool ShowGhost { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true, true, true).Write(writer);
        writer.Write(ToolIndex);
        Key.WriteRecord(writer, DeviceKey);
        writer.Write(BuildInsidePosition);
        writer.Write(BuildOutsidePosition);
        writer.WriteByteEnum(Direction);
        writer.Write(ShowGhost);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(6);
        bitField.Read(reader);
        if (bitField[0])
            ToolIndex = reader.ReadByte();
        if (bitField[1])
            DeviceKey = Key.ReadRecord(reader);
        if (bitField[2])
            BuildInsidePosition = reader.ReadVector3s();
        if (bitField[3])
            BuildOutsidePosition = reader.ReadVector3s();
        if (bitField[4])
            Direction = reader.ReadByteEnum<Direction2D>();
        if (!bitField[5])
            return;
        ShowGhost = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, BuildInfo value) => value.Write(writer);

    public static BuildInfo ReadRecord(BinaryReader reader)
    {
        var buildInfo = new BuildInfo();
        buildInfo.Read(reader);
        return buildInfo;
    }
}