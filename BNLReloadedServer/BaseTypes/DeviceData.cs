using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class DeviceData
{
    public Key DeviceKey { get; set; }

    public float TotalCost { get; set; }

    public float CostInc { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        Key.WriteRecord(writer, DeviceKey);
        writer.Write(TotalCost);
        writer.Write(CostInc);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            DeviceKey = Key.ReadRecord(reader);
        if (bitField[1])
            TotalCost = reader.ReadSingle();
        if (!bitField[2])
            return;
        CostInc = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, DeviceData value) => value.Write(writer);

    public static DeviceData ReadRecord(BinaryReader reader)
    {
        var deviceData = new DeviceData();
        deviceData.Read(reader);
        return deviceData;
    }
}