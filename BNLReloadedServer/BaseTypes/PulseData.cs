using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PulseData
{
    public float StartValue { get; set; }

    public float EndValue { get; set; }

    public int Duration { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.Write(StartValue);
        writer.Write(EndValue);
        writer.Write(Duration);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            StartValue = reader.ReadSingle();
        if (bitField[1])
            EndValue = reader.ReadSingle();
        if (!bitField[2])
            return;
        Duration = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, PulseData value) => value.Write(writer);

    public static PulseData ReadRecord(BinaryReader reader)
    {
        var pulseData = new PulseData();
        pulseData.Read(reader);
        return pulseData;
    }
}