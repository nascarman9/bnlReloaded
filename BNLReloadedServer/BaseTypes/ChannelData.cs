using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ChannelData
{
    public byte ToolIndex { get; set; }

    public Vector3 HitPos { get; set; }

    public Vector3s? TargetBlock { get; set; }

    public uint? TargetUnit { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, TargetBlock.HasValue, TargetUnit.HasValue).Write(writer);
        writer.Write(ToolIndex);
        writer.Write(HitPos);
        if (TargetBlock.HasValue)
            writer.Write(TargetBlock.Value);
        if (!TargetUnit.HasValue)
            return;
        writer.Write(TargetUnit.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        if (bitField[0])
            ToolIndex = reader.ReadByte();
        if (bitField[1])
            HitPos = reader.ReadVector3();
        TargetBlock = bitField[2] ? reader.ReadVector3s() : null;
        TargetUnit = bitField[3] ? reader.ReadUInt32() : null;
    }

    public static void WriteRecord(BinaryWriter writer, ChannelData value) => value.Write(writer);

    public static ChannelData ReadRecord(BinaryReader reader)
    {
        var channelData = new ChannelData();
        channelData.Read(reader);
        return channelData;
    }
}