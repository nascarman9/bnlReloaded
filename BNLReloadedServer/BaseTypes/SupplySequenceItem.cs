using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SupplySequenceItem
{
    public float Seconds { get; set; }

    public Key SupplyUnitKey { get; set; }

    public UnitLabel DropPointLabel { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.Write(Seconds);
        Key.WriteRecord(writer, SupplyUnitKey);
        writer.WriteByteEnum(DropPointLabel);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            Seconds = reader.ReadSingle();
        if (bitField[1])
            SupplyUnitKey = Key.ReadRecord(reader);
        if (!bitField[2])
            return;
        DropPointLabel = reader.ReadByteEnum<UnitLabel>();
    }

    public static void WriteRecord(BinaryWriter writer, SupplySequenceItem value)
    {
        value.Write(writer);
    }

    public static SupplySequenceItem ReadRecord(BinaryReader reader)
    {
        var supplySequenceItem = new SupplySequenceItem();
        supplySequenceItem.Read(reader);
        return supplySequenceItem;
    }
}