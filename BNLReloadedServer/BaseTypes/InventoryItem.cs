using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InventoryItem
{
    public ulong? PurchaseTime { get; set; }

    public ulong? EndTime { get; set; }

    public Key Item { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(PurchaseTime.HasValue, EndTime.HasValue, true).Write(writer);
        if (PurchaseTime.HasValue)
            writer.Write(PurchaseTime.Value);
        if (EndTime.HasValue)
            writer.Write(EndTime.Value);
        Key.WriteRecord(writer, Item);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        PurchaseTime = bitField[0] ? reader.ReadUInt64() : null;
        EndTime = bitField[1] ? reader.ReadUInt64() : null;
        if (!bitField[2])
            return;
        Item = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, InventoryItem value) => value.Write(writer);

    public static InventoryItem ReadRecord(BinaryReader reader)
    {
        var inventoryItem = new InventoryItem();
        inventoryItem.Read(reader);
        return inventoryItem;
    }
}