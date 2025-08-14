using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ChallengeResult
{
    public float TotalValue { get; set; }

    public int MatchesSpent { get; set; }

    public float MatchSecondsSpent { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.Write(TotalValue);
        writer.Write(MatchesSpent);
        writer.Write(MatchSecondsSpent);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            TotalValue = reader.ReadSingle();
        if (bitField[1])
            MatchesSpent = reader.ReadInt32();
        if (!bitField[2])
            return;
        MatchSecondsSpent = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ChallengeResult value)
    {
        value.Write(writer);
    }

    public static ChallengeResult ReadRecord(BinaryReader reader)
    {
        var challengeResult = new ChallengeResult();
        challengeResult.Read(reader);
        return challengeResult;
    }
}