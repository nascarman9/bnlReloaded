using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LobbyHeroLimit
{
    public LobbyHeroLimitOption LimitOption { get; set; }

    public int? Limit { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, Limit.HasValue).Write(writer);
        writer.WriteByteEnum(LimitOption);
        if (!Limit.HasValue)
            return;
        writer.Write(Limit.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            LimitOption = reader.ReadByteEnum<LobbyHeroLimitOption>();
        Limit = bitField[1] ? reader.ReadInt32() : null;
    }

    public static void WriteRecord(BinaryWriter writer, LobbyHeroLimit value)
    {
        value.Write(writer);
    }

    public static LobbyHeroLimit ReadRecord(BinaryReader reader)
    {
        var lobbyHeroLimit = new LobbyHeroLimit();
        lobbyHeroLimit.Read(reader);
        return lobbyHeroLimit;
    }
}