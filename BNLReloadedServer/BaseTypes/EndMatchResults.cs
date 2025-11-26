using BNLReloadedServer.ProtocolHelpers;
using Moserware.Skills;

namespace BNLReloadedServer.BaseTypes;

public class EndMatchResults
{
    public uint PlayerId { get; set; }
    public string GameInstanceId { get; set; } = string.Empty;
    public ulong MatchEndTime { get; set; }
    public Key MapKey { get; set; }
    public Key GameModeKey { get; set; }
    public EndMatchData MatchData { get; set; } = new();

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, true, true, true).Write(writer);
        writer.Write(PlayerId);
        writer.Write(GameInstanceId);
        writer.Write(MatchEndTime);
        Key.WriteRecord(writer, MapKey);
        Key.WriteRecord(writer, GameModeKey);
        EndMatchData.WriteRecord(writer, MatchData);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(6);
        bitField.Read(reader);
        if (bitField[0])
        {
            PlayerId = reader.ReadUInt32();
        }

        if (bitField[1])
        {
            GameInstanceId = reader.ReadString();
        }

        if (bitField[2])
        {
            MatchEndTime = reader.ReadUInt64();
        }

        if (bitField[3])
        {
            MapKey = Key.ReadRecord(reader);
        }

        if (bitField[4])
        {
            GameModeKey = Key.ReadRecord(reader);
        }

        if (bitField[5])
        {
            MatchData = EndMatchData.ReadRecord(reader);
        }
    }
    
    public static void WriteRecord(BinaryWriter writer, EndMatchResults value) => value.Write(writer);

    public static EndMatchResults ReadRecord(BinaryReader reader)
    {
        var endMatchResults = new EndMatchResults();
        endMatchResults.Read(reader);
        return endMatchResults;
    }
}