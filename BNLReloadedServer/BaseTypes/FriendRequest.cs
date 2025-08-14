using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class FriendRequest
{
    public uint PlayerId { get; set; }

    public ulong? SteamId { get; set; }

    public string? Nickname { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, SteamId.HasValue, Nickname != null).Write(writer);
        writer.Write(PlayerId);
        if (SteamId.HasValue)
            writer.Write(SteamId.Value);
        if (Nickname != null)
            writer.Write(Nickname);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            PlayerId = reader.ReadUInt32();
        SteamId = bitField[1] ? reader.ReadUInt64() : null;
        Nickname = bitField[2] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, FriendRequest value) => value.Write(writer);

    public static FriendRequest ReadRecord(BinaryReader reader)
    {
        var friendRequest = new FriendRequest();
        friendRequest.Read(reader);
        return friendRequest;
    }
}