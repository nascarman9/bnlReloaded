using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ShotData
{
    public Vector3 TargetPos { get; set; }

    public ulong? ShotId { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, ShotId.HasValue).Write(writer);
        writer.Write(TargetPos);
        if (!ShotId.HasValue)
            return;
        writer.Write(ShotId.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            TargetPos = reader.ReadVector3();
        ShotId = bitField[1] ? reader.ReadUInt64() : null;
    }

    public static void WriteRecord(BinaryWriter writer, ShotData value) => value.Write(writer);

    public static ShotData ReadRecord(BinaryReader reader)
    {
        var shotData = new ShotData();
        shotData.Read(reader);
        return shotData;
    }
}