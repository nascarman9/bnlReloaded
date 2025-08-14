using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ForceFall
{
    public float Speed { get; set; }

    public float MinHeight { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(Speed);
        writer.Write(MinHeight);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            Speed = reader.ReadSingle();
        if (!bitField[1])
            return;
        MinHeight = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ForceFall value) => value.Write(writer);

    public static ForceFall ReadRecord(BinaryReader reader)
    {
        var forceFall = new ForceFall();
        forceFall.Read(reader);
        return forceFall;
    }
}