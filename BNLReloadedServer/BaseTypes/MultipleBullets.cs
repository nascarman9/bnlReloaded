using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MultipleBullets
{
    public int Count { get; set; }

    public float? Angle { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, Angle.HasValue).Write(writer);
        writer.Write(Count);
        if (!Angle.HasValue)
            return;
        writer.Write(Angle.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            Count = reader.ReadInt32();
        Angle = bitField[1] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, MultipleBullets value)
    {
        value.Write(writer);
    }

    public static MultipleBullets ReadRecord(BinaryReader reader)
    {
        var multipleBullets = new MultipleBullets();
        multipleBullets.Read(reader);
        return multipleBullets;
    }
}