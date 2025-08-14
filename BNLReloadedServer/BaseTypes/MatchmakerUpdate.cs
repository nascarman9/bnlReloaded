using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchmakerUpdate
{
    public MatchmakerState? State { get; set; }

    public int? PlayersInQueue { get; set; }

    public float? AvgTimeInQueue { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(State != null, PlayersInQueue.HasValue, AvgTimeInQueue.HasValue).Write(writer);
        if (State != null)
            MatchmakerState.WriteRecord(writer, State);
        if (PlayersInQueue.HasValue)
            writer.Write(PlayersInQueue.Value);
        if (!AvgTimeInQueue.HasValue)
            return;
        writer.Write(AvgTimeInQueue.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        State = bitField[0] ? MatchmakerState.ReadRecord(reader) : null;
        PlayersInQueue = bitField[1] ? reader.ReadInt32() : null;
        AvgTimeInQueue = bitField[2] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchmakerUpdate value)
    {
        value.Write(writer);
    }

    public static MatchmakerUpdate ReadRecord(BinaryReader reader)
    {
        var matchmakerUpdate = new MatchmakerUpdate();
        matchmakerUpdate.Read(reader);
        return matchmakerUpdate;
    }
}