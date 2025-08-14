using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CurrencyInfo
{
    public LocalizedString? Name { get; set; }

    public string? Icon { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Name != null, Icon != null).Write(writer);
        if (Name != null)
            LocalizedString.WriteRecord(writer, Name);
        if (Icon != null)
            writer.Write(Icon);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Name = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
        Icon = bitField[1] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, CurrencyInfo value) => value.Write(writer);

    public static CurrencyInfo ReadRecord(BinaryReader reader)
    {
        var currencyInfo = new CurrencyInfo();
        currencyInfo.Read(reader);
        return currencyInfo;
    }
}