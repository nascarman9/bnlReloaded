using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SettingInfo
{
    public string? Setting { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Setting != null, OldValue != null, NewValue != null).Write(writer);
        if (Setting != null)
            writer.Write(Setting);
        if (OldValue != null)
            writer.Write(OldValue);
        if (NewValue != null)
            writer.Write(NewValue);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        Setting = bitField[0] ? reader.ReadString() : null;
        OldValue = bitField[1] ? reader.ReadString() : null;
        NewValue = bitField[2] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, SettingInfo value) => value.Write(writer);

    public static SettingInfo ReadRecord(BinaryReader reader)
    {
        var settingInfo = new SettingInfo();
        settingInfo.Read(reader);
        return settingInfo;
    }
}