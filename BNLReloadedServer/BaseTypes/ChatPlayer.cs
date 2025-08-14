using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ChatPlayer
{
    public uint PlayerId { get; set; }

    public string? Nickname { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, Nickname != null).Write(writer);
        writer.Write(PlayerId);
        if (Nickname != null)
            writer.Write(Nickname);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            PlayerId = reader.ReadUInt32();
        Nickname = bitField[1] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, ChatPlayer value) => value.Write(writer);

    public static ChatPlayer ReadRecord(BinaryReader reader)
    {
        var chatPlayer = new ChatPlayer();
        chatPlayer.Read(reader);
        return chatPlayer;
    }
}