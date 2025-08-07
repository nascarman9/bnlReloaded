using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class RegionInfo
{
    public string? Id { get; set; }

    public RegionGuiInfo? Info { get; set; }

    public string? Host { get; set; }

    public int Port { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true).Write(writer);
        writer.Write(Id!);
        RegionGuiInfo.WriteRecord(writer, Info!);
        writer.Write(Host!);
        writer.Write(Port);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        Id = !bitField[0] ? null : reader.ReadString();
        Info = !bitField[1] ? null : RegionGuiInfo.ReadRecord(reader);
        Host = !bitField[2] ? null : reader.ReadString();
        if (!bitField[3])
            return;
        Port = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, RegionInfo value) => value.Write(writer);

    public static RegionInfo ReadRecord(BinaryReader reader)
    {
        var regionInfo = new RegionInfo();
        regionInfo.Read(reader);
        return regionInfo;
    }
}