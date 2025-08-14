using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlockUpdate
{
    public ushort Id { get; set; }

    public byte Damage { get; set; }

    public ushort Vdata { get; set; }

    public byte Ldata { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true).Write(writer);
        writer.Write(Id);
        writer.Write(Damage);
        writer.Write(Vdata);
        writer.Write(Ldata);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        if (bitField[0])
            Id = reader.ReadUInt16();
        if (bitField[1])
            Damage = reader.ReadByte();
        if (bitField[2])
            Vdata = reader.ReadUInt16();
        if (!bitField[3])
            return;
        Ldata = reader.ReadByte();
    }

    public static void WriteRecord(BinaryWriter writer, BlockUpdate value) => value.Write(writer);

    public static BlockUpdate ReadRecord(BinaryReader reader)
    {
        var blockUpdate = new BlockUpdate();
        blockUpdate.Read(reader);
        return blockUpdate;
    }
}