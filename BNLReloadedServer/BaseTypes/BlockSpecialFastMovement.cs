using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlockSpecialFastMovement : BlockSpecial
{
    public override BlockSpecialType Type => BlockSpecialType.FastMovement;

    public float Distance { get; set; }

    public float Time { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(Distance);
        writer.Write(Time);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            Distance = reader.ReadSingle();
        if (!bitField[1])
            return;
        Time = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, BlockSpecialFastMovement value)
    {
        value.Write(writer);
    }

    public static BlockSpecialFastMovement ReadRecord(BinaryReader reader)
    {
        var specialFastMovement = new BlockSpecialFastMovement();
        specialFastMovement.Read(reader);
        return specialFastMovement;
    }
}