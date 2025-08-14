using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchCounterScore : MatchCounter
{
    public override MatchCounterType Type => MatchCounterType.Score;

    public ScoreType Score { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.WriteByteEnum(Score);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        Score = reader.ReadByteEnum<ScoreType>();
    }

    public static void WriteRecord(BinaryWriter writer, MatchCounterScore value)
    {
        value.Write(writer);
    }

    public static MatchCounterScore ReadRecord(BinaryReader reader)
    {
        var matchCounterScore = new MatchCounterScore();
        matchCounterScore.Read(reader);
        return matchCounterScore;
    }
}