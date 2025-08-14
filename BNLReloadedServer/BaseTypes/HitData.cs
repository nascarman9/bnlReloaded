using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class HitData
{
    public Vector3 InsidePoint { get; set; }

    public BlockShift OutsideShift { get; set; }

    public Vector3s Normal { get; set; }

    public Direction2D? Direction { get; set; }

    public uint? TargetId { get; set; }

    public bool? Crit { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, Direction.HasValue, TargetId.HasValue, Crit.HasValue).Write(writer);
        writer.Write(InsidePoint);
        writer.WriteByteEnum(OutsideShift);
        writer.Write(Normal);
        if (Direction.HasValue)
            writer.WriteByteEnum(Direction.Value);
        if (TargetId.HasValue)
            writer.Write(TargetId.Value);
        if (!Crit.HasValue)
            return;
        writer.Write(Crit.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(6);
        bitField.Read(reader);
        if (bitField[0])
            InsidePoint = reader.ReadVector3();
        if (bitField[1])
            OutsideShift = reader.ReadByteEnum<BlockShift>();
        if (bitField[2])
            Normal = reader.ReadVector3s();
        Direction = bitField[3] ? reader.ReadByteEnum<Direction2D>() : null;
        TargetId = bitField[4] ? reader.ReadUInt32() : null;
        Crit = bitField[5] ? reader.ReadBoolean() : null;
    }

    public static void WriteRecord(BinaryWriter writer, HitData value) => value.Write(writer);

    public static HitData ReadRecord(BinaryReader reader)
    {
        var hitData = new HitData();
        hitData.Read(reader);
        return hitData;
    }
}