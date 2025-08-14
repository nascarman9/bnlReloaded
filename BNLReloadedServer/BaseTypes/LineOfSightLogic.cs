using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LineOfSightLogic
{
    public float MaxDistance { get; set; } = 200f;

    public float ArcAngle { get; set; } = 30f;

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(MaxDistance);
        writer.Write(ArcAngle);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            MaxDistance = reader.ReadSingle();
        if (!bitField[1])
            return;
        ArcAngle = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, LineOfSightLogic value)
    {
        value.Write(writer);
    }

    public static LineOfSightLogic ReadRecord(BinaryReader reader)
    {
        var lineOfSightLogic = new LineOfSightLogic();
        lineOfSightLogic.Read(reader);
        return lineOfSightLogic;
    }
}