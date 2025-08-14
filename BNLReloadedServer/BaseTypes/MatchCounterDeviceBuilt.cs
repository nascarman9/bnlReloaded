using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchCounterDeviceBuilt : MatchCounter
{
    public override MatchCounterType Type => MatchCounterType.DeviceBuilt;

    public Key Device { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        Key.WriteRecord(writer, Device);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        Device = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, MatchCounterDeviceBuilt value)
    {
        value.Write(writer);
    }

    public static MatchCounterDeviceBuilt ReadRecord(BinaryReader reader)
    {
        var counterDeviceBuilt = new MatchCounterDeviceBuilt();
        counterDeviceBuilt.Read(reader);
        return counterDeviceBuilt;
    }
}