using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitCountLimit
{
    public int Limit { get; set; }

    public UnitLimitScope Scope { get; set; }

    public bool DropLast { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.Write(Limit);
        writer.WriteByteEnum(Scope);
        writer.Write(DropLast);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            Limit = reader.ReadInt32();
        if (bitField[1])
            Scope = reader.ReadByteEnum<UnitLimitScope>();
        if (!bitField[2])
            return;
        DropLast = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, UnitCountLimit value)
    {
        value.Write(writer);
    }

    public static UnitCountLimit ReadRecord(BinaryReader reader)
    {
        var unitCountLimit = new UnitCountLimit();
        unitCountLimit.Read(reader);
        return unitCountLimit;
    }
}