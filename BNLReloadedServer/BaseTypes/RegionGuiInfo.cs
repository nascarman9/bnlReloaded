using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class RegionGuiInfo
{
    public LocalizedString? Name { get; set; }

    public string? Icon { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        LocalizedString.WriteRecord(writer, Name!);
        writer.Write(Icon!);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Name = !bitField[0] ? null : LocalizedString.ReadRecord(reader);
        Icon = bitField[1] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, RegionGuiInfo value) => value.Write(writer);

    public static RegionGuiInfo ReadRecord(BinaryReader reader)
    {
        var regionGuiInfo = new RegionGuiInfo();
        regionGuiInfo.Read(reader);
        return regionGuiInfo;
    }
}