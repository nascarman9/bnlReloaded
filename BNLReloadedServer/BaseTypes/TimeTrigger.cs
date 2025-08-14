using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class TimeTrigger
{
    public DayOfWeek Day { get; set; }

    public string? Time { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, Time != null).Write(writer);
        writer.WriteByteEnum(Day);
        if (Time != null) 
            writer.Write(Time);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            Day = reader.ReadByteEnum<DayOfWeek>();
        Time = bitField[1] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, TimeTrigger value) => value.Write(writer);

    public static TimeTrigger ReadRecord(BinaryReader reader)
    {
        var timeTrigger = new TimeTrigger();
        timeTrigger.Read(reader);
        return timeTrigger;
    }
}