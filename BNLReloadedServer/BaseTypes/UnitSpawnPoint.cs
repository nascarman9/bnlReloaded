using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitSpawnPoint
{
    public float SideShift { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(SideShift);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        SideShift = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, UnitSpawnPoint value)
    {
        value.Write(writer);
    }

    public static UnitSpawnPoint ReadRecord(BinaryReader reader)
    {
        var unitSpawnPoint = new UnitSpawnPoint();
        unitSpawnPoint.Read(reader);
        return unitSpawnPoint;
    }
}