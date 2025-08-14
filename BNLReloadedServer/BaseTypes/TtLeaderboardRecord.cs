using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class TtLeaderboardRecord
{
    public uint PlayerId { get; set; }

    public string? PlayerName { get; set; }

    public float ResultSeconds { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, PlayerName != null, true).Write(writer);
        writer.Write(PlayerId);
        if (PlayerName != null)
            writer.Write(PlayerName);
        writer.Write(ResultSeconds);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            PlayerId = reader.ReadUInt32();
        PlayerName = bitField[1] ? reader.ReadString() : null;
        if (!bitField[2])
            return;
        ResultSeconds = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, TtLeaderboardRecord value)
    {
        value.Write(writer);
    }

    public static TtLeaderboardRecord ReadRecord(BinaryReader reader)
    {
        var leaderboardRecord = new TtLeaderboardRecord();
        leaderboardRecord.Read(reader);
        return leaderboardRecord;
    }
}