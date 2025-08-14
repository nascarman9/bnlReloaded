using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SupplyInfo
{
    public Key? NextSupplyDrop { get; set; }

    public ulong? NextSupplyDropTime { get; set; }

    public Vector3? Position { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(NextSupplyDrop.HasValue, NextSupplyDropTime.HasValue, Position.HasValue).Write(writer);
        if (NextSupplyDrop.HasValue)
            Key.WriteRecord(writer, NextSupplyDrop.Value);
        if (NextSupplyDropTime.HasValue)
            writer.Write(NextSupplyDropTime.Value);
        if (!Position.HasValue)
            return;
        writer.Write(Position.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        NextSupplyDrop = bitField[0] ? Key.ReadRecord(reader) : null;
        NextSupplyDropTime = bitField[1] ? reader.ReadUInt64() : null;
        Position = bitField[2] ? reader.ReadVector3() : null;
    }

    public static void WriteRecord(BinaryWriter writer, SupplyInfo value) => value.Write(writer);

    public static SupplyInfo ReadRecord(BinaryReader reader)
    {
        var supplyInfo = new SupplyInfo();
        supplyInfo.Read(reader);
        return supplyInfo;
    }
}