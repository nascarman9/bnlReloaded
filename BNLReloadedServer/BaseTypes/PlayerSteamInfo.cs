using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PlayerSteamInfo
{
    public string? Nickname { get; set; }

    public List<ulong>? Friends { get; set; }

    public string? Language { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.Write(Nickname!);
        writer.WriteList(Friends!, writer.Write);
        writer.Write(Language!);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        Nickname = !bitField[0] ? null : reader.ReadString();
        Friends = !bitField[1] ? null : reader.ReadList<ulong, List<ulong>>(reader.ReadUInt64);
        Language = !bitField[2] ? null : reader.ReadString();
    }

    public static void WriteRecord(BinaryWriter writer, PlayerSteamInfo value)
    {
        value.Write(writer);
    }

    public static PlayerSteamInfo ReadRecord(BinaryReader reader)
    {
        var playerSteamInfo = new PlayerSteamInfo();
        playerSteamInfo.Read(reader);
        return playerSteamInfo;
    }
}