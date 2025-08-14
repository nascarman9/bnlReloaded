using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class FriendInfo
{
    public uint PlayerId { get; set; }

    public ulong? SteamId { get; set; }

    public string? Nickname { get; set; }

    public bool Online { get; set; }

    public bool InMainMenu { get; set; }

    public string? Region { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, SteamId.HasValue, Nickname != null, true, true, Region != null).Write(writer);
        writer.Write(PlayerId);
        if (SteamId.HasValue)
            writer.Write(SteamId.Value);
        if (Nickname != null)
            writer.Write(Nickname);
        writer.Write(Online);
        writer.Write(InMainMenu);
        if (Region == null)
            return;
        writer.Write(Region);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(6);
        bitField.Read(reader);
        if (bitField[0])
            PlayerId = reader.ReadUInt32();
        SteamId = bitField[1] ? reader.ReadUInt64() : null;
        Nickname = bitField[2] ? reader.ReadString() : null;
        if (bitField[3])
            Online = reader.ReadBoolean();
        if (bitField[4])
            InMainMenu = reader.ReadBoolean();
        Region = bitField[5] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, FriendInfo value) => value.Write(writer);

    public static FriendInfo ReadRecord(BinaryReader reader)
    {
        var friendInfo = new FriendInfo();
        friendInfo.Read(reader);
        return friendInfo;
    }
}