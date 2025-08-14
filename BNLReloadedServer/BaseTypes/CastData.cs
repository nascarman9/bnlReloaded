using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CastData
{
    public byte ToolIndex { get; set; }

    public Vector3 ShotPos { get; set; }

    public List<ShotData>? Shots { get; set; }

    public float? UnitProjectileSpeed { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, Shots != null, UnitProjectileSpeed.HasValue).Write(writer);
        writer.Write(ToolIndex);
        writer.Write(ShotPos);
        if (Shots != null)
            writer.WriteList(Shots, ShotData.WriteRecord);
        if (!UnitProjectileSpeed.HasValue)
            return;
        writer.Write(UnitProjectileSpeed.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        if (bitField[0])
            ToolIndex = reader.ReadByte();
        if (bitField[1])
            ShotPos = reader.ReadVector3();
        Shots = bitField[2] ? reader.ReadList<ShotData, List<ShotData>>(ShotData.ReadRecord) : null;
        UnitProjectileSpeed = bitField[3] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, CastData value) => value.Write(writer);

    public static CastData ReadRecord(BinaryReader reader)
    {
        var castData = new CastData();
        castData.Read(reader);
        return castData;
    }
}